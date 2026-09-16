using MockQueryable.Moq;
using Moq;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repository;
using NotificationService.Tests.TestData;

namespace NotificationService.Tests.Mocks;

internal static class RepositoryMocks
{
    public static IMock<IBaseRepository<UserEvent>> GetMockUserEventRepository()
    {
        var mockRepository = new Mock<IBaseRepository<UserEvent>>();
        var userEvents = UserEventMother.GetUserEvents().BuildMockDbSet();

        mockRepository.Setup(x => x.GetAll()).Returns(userEvents.Object);
        mockRepository.Setup(x => x.CreateAsync(It.IsAny<UserEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEvent userEvent, CancellationToken _) => userEvent);
        mockRepository.Setup(x => x.Update(It.IsAny<UserEvent>())).Returns((UserEvent userEvent) => userEvent);
        mockRepository.Setup(x => x.Remove(It.IsAny<UserEvent>())).Returns((UserEvent userEvent) => userEvent);

        return mockRepository;
    }
}
