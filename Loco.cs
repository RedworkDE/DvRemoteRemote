namespace DvRemoteRemote
{
    public abstract class Loco<TState, TActions> : RemoteControl.ILocoWrapperBase where TState : BaseLocoState, new() where TActions : BaseLocoActions, new()
    {
        void RemoteControl.ILocoWrapperBase.GetState(object state)
        {
            GetState((TState) state);
        }

        void RemoteControl.ILocoWrapperBase.GetActions(object actions)
        {
            GetActions((TActions) actions);
        }

        public abstract void GetState(TState state);
        public abstract void GetActions(TActions actions);
    }
}