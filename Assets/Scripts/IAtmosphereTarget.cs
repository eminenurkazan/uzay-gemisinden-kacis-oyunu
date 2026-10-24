public interface IAtmosphereTarget
{
    bool CanBeAffected { get; }
    bool IsFullyInsideAirlock { get; }
    bool IsIncapacitated { get; }
}
