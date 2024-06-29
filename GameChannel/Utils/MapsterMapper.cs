using System.Collections.Generic;
using Mapster;
using PhoenixLib.DAL;

namespace GameChannel.Utils
{
    public class MapsterMapper<TEntity, TDto> : IMapper<TEntity, TDto>
    {
        public void Map(TEntity input, TDto output)
        {
            input.Adapt(output);
        }

        public TEntity Map(TDto input) => input.Adapt<TDto, TEntity>();

        public List<TEntity> Map(List<TDto> input) => input.Adapt<List<TDto>, List<TEntity>>();

        public IEnumerable<TEntity> Map(IEnumerable<TDto> input) => input.Adapt<IEnumerable<TDto>, IEnumerable<TEntity>>();

        public IReadOnlyList<TEntity> Map(IReadOnlyList<TDto> input) => input.Adapt<List<TEntity>>();

        public TDto Map(TEntity input) => input.Adapt<TEntity, TDto>();

        public List<TDto> Map(List<TEntity> input) => input.Adapt<List<TEntity>, List<TDto>>();

        public IEnumerable<TDto> Map(IEnumerable<TEntity> input) => input.Adapt<IEnumerable<TEntity>, IEnumerable<TDto>>();

        public IReadOnlyList<TDto> Map(IReadOnlyList<TEntity> input) => input.Adapt<List<TDto>>();

        public void Map(TDto input, TEntity output)
        {
            input.Adapt(output);
        }
    }
}