using System;

namespace DvRemoteRemote
{
    public class LocoShunter : Loco<ShunterLocoState, ShunterLocoActions>
    {
        private readonly LocoControllerShunter _inner;
        private readonly LocoBase _base;
        private readonly ShunterLocoSimulation _sim;

        public LocoShunter(LocoControllerShunter inner)
        {
            _inner = inner;
            _base = new LocoBase(inner);
            _sim = inner.GetComponent<ShunterLocoSimulation>();
        }

        /// <inheritdoc />
        public override void GetState(ShunterLocoState state)
        {
            _base.GetState(state);

            state.LocoType = "shunter";
            state.Sander = _inner.GetSandersOn() ? 1 : 0;
            state.SanderFlow = _inner.GetSandersFlow() / _sim.sandFlow.max;
            state.EngineTemp = _inner.GetEngineTemp();
            state.EngineOn = _inner.EngineOn;
        }

        /// <inheritdoc />
        public override void GetActions(ShunterLocoActions actions)
        {
            _base.GetActions(actions);
            actions.SetSander = val => _inner.SetSandersOn(val >= 0.5f);
            actions.SetEngineOn = on => _inner.EngineOn = on;
        }
    }

    public class ShunterLocoState : BaseLocoState
    {
        public float Sander { get; set; }
        public float SanderFlow { get; set; }
        public float EngineTemp { get; set; }
        public bool EngineOn { get; set; }
    }

    public class ShunterLocoActions : BaseLocoActions
    {
        public Action<float> SetSander { get; set; }
        public Action<bool> SetEngineOn { get; set; }
    }
}