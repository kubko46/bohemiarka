// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameChannel.Cryptography;
using NetCoreServer;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using PhoenixLib.MultiLanguage;
using WingsAPI.Communication.Sessions;
using WingsAPI.Communication.Sessions.Model;
using WingsAPI.Communication.Sessions.Request;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game._packetHandling;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Commands;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Miniland;
using WingsEmu.Game.Networking;
using WingsEmu.Packets;
using WingsEmu.Packets.ClientPackets;

namespace GameChannel.Network
{
    public class GameSession : TcpSession, IClientSession
    {
        private static ISessionManager _sessionManager;
        private static IGlobalCommandExecutor _commandsExecutor;
        private static IAsyncEventPipeline _eventPipeline;
        private static IServerManager _serverManager;
        private static ISessionService _sessionService;
        private static IMinilandManager _minilandManager;
        private static IPacketDeserializer _deserializer;

        private static readonly IPacketSerializer _serializer = new PacketSerializer();


        private static readonly char[] COMMAND_PREFIX = { '$', '%' };
        private static readonly char[] ChatDelimiter = { '/', ':', ';', '!' };


        private readonly IPacketHandlerContainer<ICharacterScreenPacketHandler> _charScreenHandlers;


        private readonly CancellationTokenSource _cts;
        private readonly IPacketHandlerContainer<IGamePacketHandler> _gameHandlers;
        private readonly IGameLanguageService _gameLanguageService;
        private readonly ConcurrentQueue<string> _packetToHandleQueue;
        private readonly ConcurrentQueue<string[]> _pendingPacketsToSend;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly IList<string> _waitForPacketList = new List<string>();

        private readonly char[] PACKET_SPLITTER = { (char)0xFF };

        private bool _isDisconnecting;

        private int _lastKeepAliveIdentity;
        private IPlayerEntity _playerEntity;

        // Packetwait Packets
        private int? _waitForPacketsAmount;

        public GameSession(GameTcpServer server, IPacketHandlerContainer<IGamePacketHandler> gameHandlers,
            IPacketHandlerContainer<ICharacterScreenPacketHandler> charScreenHandlers, IGameLanguageService gameLanguageService) : base(server)
        {
            _gameHandlers = gameHandlers;
            _charScreenHandlers = charScreenHandlers;
            _gameLanguageService = gameLanguageService;
            SessionId = 0;
            _cts = new CancellationTokenSource();
            _pendingPacketsToSend = new ConcurrentQueue<string[]>();
            _packetToHandleQueue = new ConcurrentQueue<string>();
            _ = HandlePacketLoop(_cts.Token);
            _ = SendPacketLoop(_cts.Token);
            Account_OnLangChanged(null, RegionLanguageType.CZ);
        }

        public string IpAddress { get; private set; }
        public string HardwareId { get; private set; }
        public string ClientVersion { get; private set; }

        public void SendPackets(IEnumerable<string> packets)
        {
            if (!CanReadOrSend())
            {
                return;
            }

            try
            {
                EnqueuePackets(packets.ToArray());
            }
            catch (Exception e)
            {
                Log.Error("[TCP_SESSION] SendPacket", e);
                ForceDisconnect();
            }
        }

        public void SendPacket(string packet)
        {
            if (!CanReadOrSend())
            {
                return;
            }

            if (string.IsNullOrEmpty(packet))
            {
                return;
            }

            try
            {
                EnqueuePackets(packet);
            }
            catch (Exception e)
            {
                Log.Error("SendPacket", e);
            }
        }


        public RegionLanguageType UserLanguage { get; private set; }
        public Account Account { get; private set; }

        public IPlayerEntity PlayerEntity
        {
            get
            {
                if (_playerEntity == null || !HasSelectedCharacter)
                {
                    Log.Warn("[GAME_SESSION] Uninitialized PlayerEntity cannot be accessed.");
                }

                return _playerEntity;
            }

            private set => _playerEntity = value;
        }


        public IMapInstance CurrentMapInstance { get; set; }

        public bool HasCurrentMapInstance => CurrentMapInstance != null;

        public bool HasSelectedCharacter { get; set; }
        public byte? SelectedCharacterSlot { get; set; }

        public bool IsAuthenticated { get; set; }

        public bool IsDisposing { get; set; }

        public int SessionId { get; set; }

        public bool DebugMode { get; set; }
        public bool GmMode { get; set; } = true;

        public string GetLanguage(string key) => _gameLanguageService.GetLanguage(key, UserLanguage);

        public string GetLanguageFormat(string key, params object[] formatParams) => _gameLanguageService.GetLanguageFormat(key, UserLanguage, formatParams);

        public string GetLanguage(GameDialogKey key) => _gameLanguageService.GetLanguage(key, UserLanguage);

        public string GetLanguageFormat(GameDialogKey key, params object[] formatParams) => _gameLanguageService.GetLanguageFormat(key, UserLanguage, formatParams);

        public static void Initialize(IGlobalCommandExecutor commandExecutor, IAsyncEventPipeline eventPipeline,
            IServerManager serverManager, ISessionManager sessionManager, ISessionService sessionService, IMinilandManager minilandManager, IPacketDeserializer packetDeserializer)
        {
            _sessionManager = sessionManager;
            _commandsExecutor = commandExecutor;
            _eventPipeline = eventPipeline;
            _serverManager = serverManager;
            _sessionService = sessionService;
            _minilandManager = minilandManager;
            _deserializer = packetDeserializer;
        }

        private async Task SendPacketLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    FlushPackets();

                    await Task.Delay(50, token);
                }
                catch (TaskCanceledException e)
                {
                    return;
                }
                catch (Exception e)
                {
                    ForceDisconnect();
                    Log.Error("[TCP_SESSION] HandlePacketLoop", e);
                }
            }
        }

        private void FlushPackets()
        {
            try
            {
                if (_pendingPacketsToSend.IsEmpty)
                {
                    return;
                }

                using var stream = new MemoryStream();
                while (_pendingPacketsToSend.TryDequeue(out string[] packets))
                {
                    foreach (string packet in packets)
                    {
                        byte[] bytes = WorldEncrypter.Encrypt(packet, UserLanguage.GetEncoding());
                        stream.Write(bytes);
                    }
                }

                SendAsync(stream.ToArray());
            }
            catch (Exception e)
            {
                Log.Error("[TCP_SESSION] FlushPackets", e);
            }
        }

        private async Task HandlePacketLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_packetToHandleQueue.IsEmpty)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                if (!_packetToHandleQueue.TryDequeue(out string buff))
                {
                    await Task.Delay(50, token);
                    continue;
                }

                bool isHandling = false;
                try
                {
                    isHandling = await _semaphoreSlim.WaitAsync(0, token);
                    if (isHandling)
                    {
                        OnGamePacketReceived(buff);
                    }
                }
                catch (TaskCanceledException e)
                {
                    return;
                }
                catch (Exception e)
                {
                    ForceDisconnect();
                    Log.Error("[TCP_SESSION] HandlePacketLoop", e);
                }
                finally
                {
                    if (isHandling)
                    {
                        _semaphoreSlim.Release();
                    }
                }

                await Task.Delay(50, token);
            }

            _semaphoreSlim?.Dispose();
        }

        private void EnqueuePackets(string packet)
        {
            _pendingPacketsToSend.Enqueue(new[] { packet });
        }

        private void EnqueuePackets(string[] packets)
        {
            _pendingPacketsToSend.Enqueue(packets);
        }

        private void OnGamePacketReceived(string e)
        {
            try
            {
                HandlePackets(e);
            }
            catch (Exception ex)
            {
                Log.Error("Client_OnPacketReceived : ", ex);
            }
        }

        protected override void OnConnected()
        {
            try
            {
                if (IsDisposed)
                {
                    return;
                }

                if (IsSocketDisposed)
                {
                    ForceDisconnect();
                    return;
                }

                if (Socket == null)
                {
                }
            }
            catch (Exception e)
            {
                Log.Error("[WORLD_SERVER_SESSION] OnConnected", e);
                ForceDisconnect();
            }
        }

        protected override void OnDisconnected()
        {
            ForceDisconnect();
        }

        private bool CanReadOrSend()
        {
            if (IsDisposing)
            {
                return false;
            }

            return !_serverManager.InShutdown;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (!CanReadOrSend())
            {
                return;
            }

            try
            {
                string buff = WorldDecrypter.Decrypt(buffer.AsSpan((int)offset, (int)size), SessionId, UserLanguage.GetEncoding());
                var packets = buff.Split(PACKET_SPLITTER, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (HasSelectedCharacter)
                {
                    foreach (string packet in packets)
                    {
                        _packetToHandleQueue.Enqueue(packet);
                    }

                    return;
                }

                foreach (string packet in packets)
                {
                    OnGamePacketReceived(packet);
                }
            }
            catch (Exception e)
            {
                Log.Error("[TCP_SESSION] OnReceived", e);
                ForceDisconnect();
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error("[TCP_SESSION]", new Exception($"OnError {error}"));
            ForceDisconnect();
        }

        #region Methods

        public void ForceDisconnect()
        {
            if (_isDisconnecting)
            {
                Log.Debug("[TCP_SESSION] Already disconnecting...");
                return;
            }

            try
            {
                Log.Debug("[TCP_SESSION] Force disconnecting...");
                IsDisposing = true;
                _isDisconnecting = true;
                Log.Debug("[TCP_SESSION] Flushing packets...");
                FlushPackets();
                Log.Debug("[TCP_SESSION] Packets flushed...");
                _pendingPacketsToSend.Clear();
                _packetToHandleQueue.Clear();
                _cts.Cancel();
                if (Account != null)
                {
                    Log.Debug("[TCP_SESSION] Removing Account delegates...");
                    Account.LangChanged -= Account_OnLangChanged;
                }

                // do everything necessary before removing client, DB save, Whatever
                if (!HasSelectedCharacter)
                {
                    Log.Debug("[TCP_SESSION] No character selected...");
                    if (Account != null)
                    {
                        Log.Debug("[TCP_SESSION] Account not null...");
                        _sessionService.Disconnect(new DisconnectSessionRequest
                        {
                            AccountId = Account.Id,
                            EncryptionKey = SessionId
                        }).ConfigureAwait(false).GetAwaiter().GetResult();
                    }

                    Log.Debug($"[TCP_SESSION] {IpAddress} disconnected without selecting character");
                    Disconnect();
                    return;
                }

                Log.Debug("[TCP_SESSION] Clearing buffs...");
                PlayerEntity.BuffComponent.ClearNonPersistentBuffs();
                Log.Debug("[TCP_SESSION] Unregistering session from MapInstance...");
                // CurrentMapInstance?.UnregisterSession(this);
                Log.Debug("[TCP_SESSION] Saving Character...");
                this.CharacterDisconnect().ConfigureAwait(false).GetAwaiter().GetResult();
                Log.Debug("[TCP_SESSION] Unregistering session from SessionManager...");
                _sessionManager.UnregisterSession(this);
                Log.Debug("[TCP_SESSION] Removing session from Master...");
                _minilandManager.RemoveMiniland(PlayerEntity.Id);
                Log.Debug("[TCP_SESSION] Unregistering Miniland...");
                if (Account != null)
                {
                    _sessionService.Disconnect(new DisconnectSessionRequest
                    {
                        AccountId = Account.Id,
                        EncryptionKey = SessionId
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                Log.Info($"[TCP_SESSION] {PlayerEntity.Name} - {IpAddress} disconnected");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error("[TCP_SESSION] Session could not disconnect properly", e);
            }
        }

        public void InitializeAccount(Account account, Session session)
        {
            Account = account;
            IpAddress = session.IpAddress;
            HardwareId = session.HardwareId;
            ClientVersion = session.ClientVersion;
            Account_OnLangChanged(null, account.Language.ToRegionLanguageType());
            Account.LangChanged += Account_OnLangChanged;
            IsAuthenticated = true;
        }

        private void Account_OnLangChanged(object sender, RegionLanguageType e)
        {
            UserLanguage = e;
        }

        //[Obsolete("Primitive string operations will be removed in future, use PacketDefinition SendPacket instead. SendPacket with string parameter should only be used for debugging.")]
        public void SendPacket<T>(T packet) where T : IPacket
        {
            if (IsDisposing)
            {
                return;
            }

            SendPacket(_serializer.Serialize(packet));
        }


        public void InitializePlayerEntity(IPlayerEntity character)
        {
            HasSelectedCharacter = true;
            PlayerEntity = character;
            PlayerEntity.SetSession(this);
            _sessionManager.RegisterSession(this);
        }

        public void EmitEvent<T>(T e) where T : PlayerEvent
        {
            EmitEventAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task EmitEventAsync<T>(T e) where T : PlayerEvent
        {
            e.Sender = this;
            await _eventPipeline.ProcessEventAsync(e).ConfigureAwait(false);
        }

        private void ProcessUnAuthedPacket(string sessionPacket)
        {
            if (string.IsNullOrEmpty(sessionPacket))
            {
                return;
            }

            string[] sessionParts = sessionPacket.Split(' ');
            if (sessionParts.Length == 0)
            {
                return;
            }

            if (!int.TryParse(sessionParts[0], out int packetId))
            {
                ForceDisconnect();
                return;
            }

            _lastKeepAliveIdentity = packetId;

            // set the SessionId if Session Packet arrives
            if (sessionParts.Length < 2)
            {
                return;
            }

            if (!int.TryParse(sessionParts[1].Split('\\').FirstOrDefault(), out int sessionId))
            {
                return;
            }

            SessionId = sessionId;

            if (_waitForPacketsAmount.HasValue)
            {
                return;
            }

            _waitForPacketsAmount = 3;
            _waitForPacketList.Add(EntryPointPacket.EntryPointPacketHeader);
        }

        /// <summary>
        ///     Handle the packet received by the Client.
        /// </summary>
        private void HandlePackets(string packet)
        {
            // determine first packet
            if (SessionId == 0)
            {
                ProcessUnAuthedPacket(packet);
                return;
            }

            string packetString = packet.Replace('^', ' ');
            string[] packetSplit = packetString.Split(' ');

            if (_waitForPacketsAmount.HasValue)
            {
                WaitForEntrypointPackets(packetSplit);
                return;
            }

            ProcessWorldPackets(packetSplit, packetString, packet);
        }

        private void ProcessWorldPackets(IList<string> packetSplit, string packetString, string packet)
        {
            // keep alive
            string nextKeepAliveRaw = packetSplit[0];
            if (!int.TryParse(nextKeepAliveRaw, out int nextKeepaliveIdentity) && nextKeepaliveIdentity != (_lastKeepAliveIdentity + 1))
            {
                Log.Warn("CORRUPTED_KEEPALIVE " + IpAddress);
                ForceDisconnect();
                return;
            }

            if (nextKeepaliveIdentity == 0)
            {
                if (_lastKeepAliveIdentity == ushort.MaxValue)
                {
                    _lastKeepAliveIdentity = nextKeepaliveIdentity;
                }
            }
            else
            {
                _lastKeepAliveIdentity = nextKeepaliveIdentity;
            }


            if (packetSplit.Count <= 1)
            {
                return;
            }

            if (packetSplit[1].Length < 1)
            {
                return;
            }

            if (COMMAND_PREFIX.Any(s => s == packetSplit[1][0]))
            {
                _commandsExecutor.HandleCommand(packet.Substring(packet.IndexOf(' ', StringComparison.OrdinalIgnoreCase) + 1), this, packetSplit[1][0].ToString());
                return;
            }

            if (packetSplit[1].Length >= 1 && ChatDelimiter.Any(s => s == packetSplit[1][0]))
            {
                packetSplit[1] = packetSplit[1][0].ToString();
                packetString = packet.Insert(packet.IndexOf(' ', StringComparison.OrdinalIgnoreCase) + 2, " ");
            }

            if (packetSplit[1] != "0")
            {
                TriggerHandler(packetSplit[1].Replace("#", "", StringComparison.OrdinalIgnoreCase), packetString);
            }
        }

        private void WaitForEntrypointPackets(IReadOnlyList<string> packetSplit)
        {
            if (packetSplit.Count < 2)
            {
                return;
            }

            // cross server authentication
            if (packetSplit.Count > 3 && packetSplit[1] == "DAC")
            {
                _waitForPacketList.Clear();
                _waitForPacketList.Add(string.Join(" ", packetSplit.Skip(1).ToArray()));
                _waitForPacketsAmount = 1;
            }
            else
            {
                // username or password
                _waitForPacketList.Add(packetSplit[1]);
            }

            if (_waitForPacketList.Count != _waitForPacketsAmount)
            {
                return; // continue;
            }

            _waitForPacketsAmount = null;
            string queuedPackets = string.Join(" ", _waitForPacketList.ToArray());
            string header = queuedPackets.Split(' ', '^')[0];
            TriggerHandler(header, queuedPackets);
            _waitForPacketList.Clear();
        }

        private void TriggerHandler(string packetHeader, string packetString)
        {
            if (_serverManager.InShutdown)
            {
                return;
            }

            if (IsDisposing)
            {
                Log.Warn("[CLIENTSESSION] DISPOSING");
                return;
            }

            try
            {
                (IClientPacket typedPacket, Type packetType) = _deserializer.Deserialize(packetString, IsAuthenticated);

                if (packetType == typeof(UnresolvedPacket) && typedPacket != null)
                {
                    Log.Warn($"AccountId: {(Account?.Id ?? 0).ToString()} UNRESOLVED_PACKET : {packetHeader}");
                    return;
                }

                if (packetType == null && typedPacket == null)
                {
                    Log.Info("DESERIALIZATION_ERROR");
                    return;
                }

                if (HasSelectedCharacter)
                {
                    _gameHandlers.Execute(this, typedPacket, packetType);
                }
                else
                {
                    _charScreenHandlers.Execute(this, typedPacket, packetType);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Handler Error]", ex);
            }
        }

        #endregion
    }
}