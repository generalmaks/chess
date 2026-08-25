using Chess.Model.Player;
using Moq;

namespace Chess.Orchestrator.Tests.Support;

public class MockPlayerRepository
{
    public Mock<IPlayerRepository> Mock { get; } = new();
    public IPlayerRepository Object => Mock.Object;

    public List<Model.Player.Player> AddedPlayers { get; } = [];

    public MockPlayerRepository()
    {
        Mock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((username, _) =>
                Task.FromResult(AddedPlayers.SingleOrDefault(p => p.Username == username)));

        Mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, CancellationToken>((id, _) =>
                Task.FromResult(AddedPlayers.SingleOrDefault(p => p.Id == id)));

        Mock.Setup(r => r.AddPlayerAsync(It.IsAny<Model.Player.Player>(), It.IsAny<CancellationToken>()))
            .Callback<Model.Player.Player, CancellationToken>((player, _) => AddedPlayers.Add(player))
            .Returns(Task.CompletedTask);

        Mock.Setup(r => r.UpdateRatingAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, int, CancellationToken>((playerId, newRating, _) =>
                AddedPlayers.Single(p => p.Id == playerId).EloRating = newRating)
            .Returns(Task.CompletedTask);
    }
}
