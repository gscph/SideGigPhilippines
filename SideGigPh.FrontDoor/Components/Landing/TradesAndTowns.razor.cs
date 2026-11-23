namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// What and where: the trades as big tiles, with a pill to switch to Construction's jobs, what each trade covers
/// (<see cref="ServiceList"/>), then the towns as cards, Angono first. No map: it would promise coverage we don't have.
/// </summary>
public partial class TradesAndTowns
{
    private bool _construction;

    private Trade[] Tiles => _construction ? TradeList.ConstructionJobs : TradeList.Trades;
}
