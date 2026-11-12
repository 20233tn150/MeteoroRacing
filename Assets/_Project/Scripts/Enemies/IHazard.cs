namespace MeteoroRacing
{
    /// <summary>Cualquier peligro que pueda impactar contra el Mach 5.</summary>
    public interface IHazard
    {
        /// <summary>True si el Auto-Jacks permite saltarlo.</summary>
        bool IsGroundHazard { get; }

        /// <summary>Se llama cuando el jugador toca el peligro estando en el suelo.</summary>
        void HitPlayer(Mach5Controller player);
    }
}
