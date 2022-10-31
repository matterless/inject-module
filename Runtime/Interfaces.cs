namespace Matterless.Inject
{
    public interface IInitializable
    {
        void Initialize();
    }

    public interface ITickable
    {
        void Tick(float deltaTime, float unscaledDeltaTime);
    }

    public interface ILateTickable
    {
        void LateTick(float deltaTime, float unscaledDeltaTime);
    }

    public interface IFixedTickable
    {
        void FixedTick(float fixedDeltaTime, float fixedUnscaledDeltaTime);
    }
}