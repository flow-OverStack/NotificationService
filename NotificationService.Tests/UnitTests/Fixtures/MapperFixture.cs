using AutoMapper;
using NotificationService.Application.Mappings;

namespace NotificationService.Tests.UnitTests.Fixtures;

internal static class MapperFixture
{
    public static IMapper GetMapperConfiguration()
    {
        var mockMapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(UserEventMapping)));
        return mockMapper.CreateMapper();
    }
}
