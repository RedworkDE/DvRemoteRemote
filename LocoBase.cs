using System;
using JetBrains.Annotations;

namespace DvRemoteRemote
{
    public class LocoBase : Loco<BaseLocoState, BaseLocoActions>
    {
        [NotNull] private readonly LocoControllerBase _inner;

        public LocoBase([NotNull] LocoControllerBase inner)
        {
            _inner = inner;
        }

        public override void GetState([NotNull] BaseLocoState state)
        {
            state.Reverser = _inner.reverser;
            state.ReverserSymbol = _inner.GetReverserSymbol();
            state.Throttle = (float) Math.Round(_inner.throttle, 2);
            state.TargetThrottle = (float) Math.Round(_inner.targetThrottle, 2);
            state.Break = (float) Math.Round(_inner.brake, 2);
            state.TargetBreak = (float) Math.Round(_inner.targetBrake, 2);
            state.Derailed = _inner.IsDerailed();
            state.WheelSlip = _inner.IsWheelslipping();
            state.Speed = (float) Math.Round(_inner.GetSpeedKmH(), 2);
            state.CanCouple = _inner.IsCouplerInRange();
            state.MinCouplePos = -_inner.GetNumberOfCarsInRear() - 1;
            state.MaxCouplePos = _inner.GetNumberOfCarsInFront() + 1;
            state.LocoType = "base";
        }

        public override void GetActions([NotNull] BaseLocoActions actions)
        {
            actions.SetThrottle = _inner.SetThrottle;
            actions.SetBreak = _inner.SetBrake;
            actions.SetReverser = _inner.SetReverser;
            actions.Couple = _inner.Couple;
            actions.UnCouple = _inner.Uncouple;
        }
    }

    public class BaseLocoState
    {
        public string LocoType { get; set; }
        public float Throttle { get; set; }
        public float TargetThrottle { get; set; }
        public float Break { get; set; }
        public float TargetBreak { get; set; }
        public float Reverser { get; set; }
        public string ReverserSymbol { get; set; }
        public bool Derailed { get; set; }
        public bool WheelSlip { get; set; }
        public float Speed { get; set; }
        public int MinCouplePos { get; set; }
        public int MaxCouplePos { get; set; }
        public bool CanCouple { get; set; }
    }

    public class BaseLocoActions
    {
        public Action<float> SetThrottle { get; set; }
        public Action<float> SetBreak { get; set; }
        public Action<float> SetReverser { get; set; }
        public Action<int> Couple { get; set; }
        public Action<int> UnCouple { get; set; }
    }
}