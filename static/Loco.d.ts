export interface BaseState {
    Throttle: number;
    TargetThrottle: number;
    Break: number;
    TargetBreak: number;
    Reverser: number;
    ReverserSymbol: string;
    Derailed: boolean;
    WheelSlip: boolean;
    Speed: number;
    MinCouplePos: number;
    MaxCouplePos: number;
    CanCouple: boolean;
}

export interface BaseLocoState extends BaseState {
    LocoType: 'base';
}

export interface ShunterLocoState extends BaseState {
    LocoType: 'shunter';
    Sander: number;
    SanderFlow: number;
    EngineTemp: number;
    EngineOn: boolean;
}

export type LocoState = BaseLocoState | ShunterLocoState;
export type Events<T> = { [P in keyof T]: Promise<T[P]> } & { resolve: { [P in keyof T]: (value?: T[P] | PromiseLike<T[P]>) => void } };
export type LocoStateSum = BaseLocoState & ShunterLocoState;

export interface ActionsBase {
    SetThrottle(value: number): void;
    SetBreak(value: number): void;
    SetReverser(value: number): void;
    Couple(value: number): void;
    UnCouple(value: number): void;
}

export interface BaseLocoActions extends ActionsBase {
    LocoType: 'base';
}

export interface ShunterLocoActions extends ActionsBase {
    LocoType: 'shunter';
    SetSander(value: number): void;
    SetEngineOn(value: boolean): void;
}

export type LocoActions = BaseLocoActions | ShunterLocoActions;