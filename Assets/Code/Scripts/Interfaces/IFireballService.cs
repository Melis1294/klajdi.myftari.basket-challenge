public interface IFireballService
{
    int FireballMultiplier { get; }
    void AddScore(float amount);
    void OnMissedShot();
}
