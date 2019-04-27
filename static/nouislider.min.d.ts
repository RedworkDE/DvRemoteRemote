declare const noUiSlider: NoUiSlider;

interface NoUiSlider {
    version: string;
    create<T extends HTMLElement>(target: Slider<T>, originalOptions: Options) : Api<T>;
}

type Slider<T extends HTMLElement> = T & { noUiSlider: Api<T> };

interface Options {
    start: number | number[];
    range: { min: number, max: number, [point: string]: number };
    step?: number;
    snap?: boolean;
    format?: Formatter;
    ariaFormat?: Formatter;
    connect?: boolean | boolean[];
    margin?: number;
    limit?: number;
    padding?: number | [number] | [number, number];
    orientation?: 'vertical' | 'horizontal';
    direction?: 'ltr' | 'rtl';
    tooltips?: boolean | Formatter | (boolean | Formatter)[];
    animate?: boolean;
    animationDuration?: number;
    keyboardSupport?: boolean;
    behaviour?: keyof typeof BehaviourValues | string;
    pips?: Pips;
    documentElement?: HTMLElement;
    cssPrefix?: string;
    cssClasses?: ClassList;
}

declare enum BehaviourValues {
    "",
    "drag",
    "tap",
    "fixed",
    "snap",
    "hover",
    "unconstrained",
    "drag tap",
    "drag snap",
    "drag hover",
    "drag unconstrained",
    "tap snap",
    "tap hover",
    "tap unconstrained",
    "snap hover",
    "snap unconstrained",
    "hover unconstrained",
    "drag tap snap",
    "drag tap hover",
    "drag tap unconstrained",
    "drag snap hover",
    "drag snap unconstrained",
    "drag hover unconstrained",
    "tap snap hover",
    "tap snap unconstrained",
    "tap hover unconstrained",
    "snap hover unconstrained",
    "drag tap snap hover",
    "drag tap snap unconstrained",
    "drag tap hover unconstrained",
    "drag snap hover unconstrained",
    "tap snap hover unconstrained",
    "drag tap snap hover unconstrained"
}

interface ClassList {
    target: string;
    base: string;
    origin: string;
    handle: string;
    handleLower: string;
    handleUpper: string;
    touchArea: string;
    horizontal: string;
    vertical: string;
    background: string;
    connect: string;
    connects: string;
    ltr: string;
    rtl: string;
    draggable: string;
    drag: string;
    tap: string;
    active: string;
    tooltip: string;
    pips: string;
    pipsHorizontal: string;
    pipsVertical: string;
    marker: string;
    markerHorizontal: string;
    markerVertical: string;
    markerNormal: string;
    markerLarge: string;
    markerSub: string;
    value: string;
    valueHorizontal: string;
    valueVertical: string;
    valueNormal: string;
    valueLarge: string;
    valueSub: string;
}

interface BasePips {
    filter?(value: number, type: 0 | 1 | 2) : -1 | 0 | 1 | 2 | number; // | number should not be here, but removing it will cause various non descriptive errors when declaring options
    density?: number;
    format?: Formatter;
}

interface SimplePips extends BasePips {
    mode: 'steps' | 'range';
}

interface CountPips {
    mode: 'count';
    values: number;
    stepped?: boolean;
}

interface NumbersPips {
    mode: 'positions' | 'values';
    values: number[];
    stepped?: boolean;
}

type Pips = SimplePips | CountPips | NumbersPips;

interface Api<T extends HTMLElement> {
    destroy(): void;
    steps(): [number | false | null, number | false | null][];
    on(event: 'start' | 'slide' | 'update' | 'change' | 'set' | 'end' | string,
        handler: (this: Api<T>,
            values: string[],
            handle: number,
            unencoded: number[],
            tap: boolean,
            positions: number[]) => void): void;
    get(): string | string[];
    set(value: number | string | (number | string | null)[], fireSetEvent?: boolean): void;
    setHandle(handle: number, value: number | string, fireSetEvent?: boolean): void;
    reset(): void;
    updateOptions(options: Partial<Options>): void;
    pips(pips: Pips): void;
    removePips(): void;
    removeTooltips(): void;

    readonly options: Readonly<Options>;
    readonly target: T;
}

interface Formatter {
    from(val:string): number;
    to(num: number): string;
}