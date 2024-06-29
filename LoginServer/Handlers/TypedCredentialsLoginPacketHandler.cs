// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Linq;
using System.Threading.Tasks;
using LoginServer.Auth;
using LoginServer.Network;
using PhoenixLib.Logging;
using PhoenixLib.MultiLanguage;
using WingsAPI.Communication;
using WingsAPI.Communication.DbServer.AccountService;
using WingsAPI.Communication.ServerApi;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsAPI.Communication.Sessions;
using WingsAPI.Communication.Sessions.Model;
using WingsAPI.Communication.Sessions.Request;
using WingsAPI.Communication.Sessions.Response;
using WingsAPI.Data.Account;
using WingsEmu.DTOs.Account;
using WingsEmu.Health;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;

namespace LoginServer.Handlers
{
    public class TypedCredentialsLoginPacketHandler : GenericLoginPacketHandlerBase<Nos0575Packet>
    {
        private readonly IAccountService _accountService;
        private readonly IMaintenanceManager _maintenanceManager;
        private readonly IServerApiService _serverApiService;
        private readonly ISessionService _sessionService;

        public TypedCredentialsLoginPacketHandler(ISessionService sessionService, IServerApiService serverApiService, IMaintenanceManager maintenanceManager, IAccountService accountService)
        {
            _sessionService = sessionService;
            _serverApiService = serverApiService;
            _maintenanceManager = maintenanceManager;
            _accountService = accountService;
        }


        protected override async Task HandlePacketAsync(LoginClientSession session, Nos0575Packet nos0575Packet)
        {
            if (nos0575Packet == null)
            {
                return;
            }

            AccountLoadResponse accountLoadResponse = null;
            try
            {
                accountLoadResponse = await _accountService.LoadAccountByName(new AccountLoadByNameRequest
                {
                    Name = nos0575Packet.Name
                });
            }
            catch (Exception e)
            {
                Log.Error("[NEW_TYPED_AUTH] Unexpected error: ", e);
            }

            if (accountLoadResponse?.ResponseType != RpcResponseType.SUCCESS)
            {
                Log.Warn($"[NEW_TYPED_AUTH] Failed to load account for accountName: '{nos0575Packet.Name}'");
                session.SendPacket(session.GenerateFailcPacket(LoginFailType.AccountOrPasswordWrong));
                session.Disconnect();
                return;
            }

            AccountDTO loadedAccount = accountLoadResponse.AccountDto;

            Log.Info($"[DEBUG] SHA-512 Password: {nos0575Packet.Password}");
            Log.Info($"[DEBUG] Stored Sha512Bcrypt Hash: {loadedAccount.Password}");


            string lowercasesha512nos = nos0575Packet.Password.ToLower();

            Log.Info($"[DEBUG] SHA-512 pass nosko lowercase: {lowercasesha512nos}");

            if (!BCrypt.Net.BCrypt.Verify(lowercasesha512nos, loadedAccount.Password))
            {
                session.SendPacket(session.GenerateFailcPacket(LoginFailType.AccountOrPasswordWrong));
                Log.Warn($"[NEW_TYPED_AUTH] WRONG_CREDENTIALS : {loadedAccount.Name}");
                session.Disconnect();
                return;
            }

            SessionResponse modelResponse = await _sessionService.CreateSession(new CreateSessionRequest
            {
                AccountId = loadedAccount.Id,
                AccountName = loadedAccount.Name,
                AuthorityType = loadedAccount.Authority,
                IpAddress = session.IpAddress
            });

            if (modelResponse.ResponseType != RpcResponseType.SUCCESS)
            {
                Log.Debug($"[NEW_TYPED_AUTH] FAILED TO CREATE SESSION {loadedAccount.Id}");
                session.SendPacket(session.GenerateFailcPacket(LoginFailType.AlreadyConnected));
                session.Disconnect();
                return;
            }

            AuthorityType type = loadedAccount.Authority;

            AccountBanGetResponse banResponse = null;
            try
            {
                banResponse = await _accountService.GetAccountBan(new AccountBanGetRequest
                {
                    AccountId = loadedAccount.Id
                });
            }
            catch (Exception e)
            {
                Log.Error("[NEW_TYPED_AUTH] Unexpected error: ", e);
            }

            if (banResponse?.ResponseType != RpcResponseType.SUCCESS)
            {
                Log.Warn($"[NEW_TYPED_AUTH] Failed to get account ban for accountId: '{loadedAccount.Id.ToString()}'");
                session.SendPacket(session.GenerateFailcPacket(LoginFailType.UnhandledError));
                session.Disconnect();
                return;
            }

            AccountBanDto characterPenalty = banResponse.AccountBanDto;
            if (characterPenalty != null)
            {
                session.SendPacket(session.GenerateFailcPacket(LoginFailType.Banned));
                Log.Debug($"[NEW_TYPED_AUTH] ACCOUNT_BANNED : {loadedAccount.Name}");
                session.Disconnect();
                return;
            }

            switch (type)
            {
                case AuthorityType.Banned:
                    session.SendPacket(session.GenerateFailcPacket(LoginFailType.Banned));
                    Log.Debug("[NEW_TYPED_AUTH] ACCOUNT_BANNED");
                    session.Disconnect();
                    break;

                case AuthorityType.Unconfirmed:
                case AuthorityType.Closed:
                    session.SendPacket(session.GenerateFailcPacket(LoginFailType.CantConnect));
                    Log.Debug("[NEW_TYPED_AUTH] ACCOUNT_NOT_VERIFIED");
                    session.Disconnect();
                    break;

                default:
                    if (_maintenanceManager.IsMaintenanceActive && loadedAccount.Authority < AuthorityType.GameMaster)
                    {
                        session.SendPacket(session.GenerateFailcPacket(LoginFailType.Maintenance));
                        return;
                    }

                    SessionResponse connectResponse = await _sessionService.ConnectToLoginServer(new ConnectToLoginServerRequest
                    {
                        AccountId = loadedAccount.Id,
                        ClientVersion = "BYPASS",
                        HardwareId = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
                    });

                    if (connectResponse.ResponseType != RpcResponseType.SUCCESS)
                    {
                        Log.Warn("[NEW_AUTH] General Error SessionId: " + session.Id);
                        session.SendPacket(session.GenerateFailcPacket(LoginFailType.CantConnect));
                        session.Disconnect();
                        return;
                    }

                    Session connectedSession = connectResponse.Session;

                    Log.Debug($"[NEW_TYPED_AUTH] Connected : {nos0575Packet.Name}:{connectedSession.EncryptionKey}:{connectedSession.HardwareId}");

                    RetrieveRegisteredWorldServersResponse worldServersResponse = await _serverApiService.RetrieveRegisteredWorldServers(new RetrieveRegisteredWorldServersRequest
                    {
                        RequesterAuthority = loadedAccount.Authority
                    });

                    if (worldServersResponse?.WorldServers is null || !worldServersResponse.WorldServers.Any())
                    {
                        session.SendPacket(session.GenerateFailcPacket(LoginFailType.Maintenance));
                        session.Disconnect();
                        return;
                    }

                    session.SendChannelPacketList(connectedSession.EncryptionKey, loadedAccount.Name, RegionLanguageType.CZ, worldServersResponse.WorldServers, true);
                    session.Disconnect();
                    break;
            }
        }
    }
}