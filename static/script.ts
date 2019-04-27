import "./nouislider.min";
import { LocoState, LocoStateSum, LocoActions, Events } from "./Loco";

const delay = (ms: number) => new Promise<void>(res => setTimeout(res, ms));

const throttleCtrl = document.getElementById("throttle") as Slider<HTMLDivElement>;
const breakCtrl = document.getElementById("break") as Slider<HTMLDivElement>;
const reverserCtrl = document.getElementById("reverser") as Slider<HTMLDivElement>;
const couplerCtrl = document.getElementById("coupler") as Slider<HTMLDivElement>;
const speedCtrl = document.getElementById("speed") as Slider<HTMLDivElement>;
const engineTempCtrl = document.getElementById("engineTemp") as Slider<HTMLDivElement>;
const engineOnCtrl = document.getElementById("engineOn") as Slider<HTMLDivElement>;
const sanderCtrl = document.getElementById("sander") as Slider<HTMLDivElement>;
const stateContainer = document.getElementById("stateContainer") as HTMLDivElement;
const detailsBtn = document.getElementById("showDetails") as HTMLButtonElement;
const coupleBtn = document.getElementById("couple") as HTMLButtonElement;
const uncoupleBtn = document.getElementById("uncouple") as HTMLButtonElement;
const targetSpeedChk = document.getElementById("targetSpeed") as HTMLInputElement;
const targetRangeChk = document.getElementById("targetSpeedRange") as HTMLInputElement;
const autoSanderChk = document.getElementById("autoSander") as HTMLInputElement;

let clientId: number | undefined = undefined;
const getClient = (async () => clientId = await (await fetch("register")).json())();
let state: (LocoState & { UpdateNum?: number });
let stateChanged: Events<LocoState>;
let actions: LocoActions;
let autoSpeedMode = 0;

noUiSlider.create(throttleCtrl,
    {
        range: { min: 0, max: 1 },
        start: [0.5, 0.5],
        behaviour: 'drag tap unconstrained',
        connect: [true, false, false],
    });
throttleCtrl.getElementsByClassName('noUi-origin')[0].setAttribute("disabled", "");
style(throttleCtrl.getElementsByClassName('noUi-origin')[0]).display = "none";
noUiSlider.create(breakCtrl,
    {
        range: { min: 0, max: 1 },
        start: [0.5, 0.5],
        behaviour: 'drag tap unconstrained',
        connect: [true, false, false],
    });
breakCtrl.getElementsByClassName('noUi-origin')[0].setAttribute("disabled", "");
style(breakCtrl.getElementsByClassName('noUi-origin')[0]).display = "none";
noUiSlider.create(reverserCtrl,
    {
        range: { min: -1, max: 1 },
        step: 1,
        start: 1,
        behaviour: 'drag tap',
    });
noUiSlider.create(couplerCtrl,
    {
        range: { min: -1, max: 1 },
        step: 1,
        padding: 0,
        start: 0,
        behaviour: 'drag tap',
        pips: {
            mode: 'steps',
            filter: value => ((Math.round(value) !== value) ? -1 : (value === 0 ? 1 : 2)),
            density: 1,
        },
    });
noUiSlider.create(speedCtrl,
    {
        range: { min: 0, max: 120 },
        start: [0, 0, 120],
        behaviour: 'drag tap unconstrained',
        connect: [true, false, false, false],
        pips: {
            mode: 'steps',
            filter: value => [20, 40, 80].indexOf(Math.round(value)) !== -1 ? 1 : Math.round(value) % 10 ? 0 : 2,
            density: 1,
        }
    });
speedCtrl.getElementsByClassName('noUi-origin')[0].setAttribute("disabled", "");
speedCtrl.getElementsByClassName('noUi-origin')[1].setAttribute("disabled", "");
speedCtrl.getElementsByClassName('noUi-origin')[2].setAttribute("disabled", "");
style(speedCtrl.getElementsByClassName('noUi-origin')[0]).display = "none";
style(speedCtrl.getElementsByClassName('noUi-origin')[1]).display = "none";
style(speedCtrl.getElementsByClassName('noUi-origin')[2]).display = "none";
noUiSlider.create(engineTempCtrl,
    {
        range: { min: 30, max: 120 },
        start: [45, 60, 90, 105],
        behaviour: 'unconstrained',
        connect: [true, false, false, true, true],
    });
engineTempCtrl.getElementsByClassName('noUi-origin')[0].setAttribute("disabled", "");
engineTempCtrl.getElementsByClassName('noUi-origin')[1].setAttribute("disabled", "");
engineTempCtrl.getElementsByClassName('noUi-origin')[2].setAttribute("disabled", "");
engineTempCtrl.getElementsByClassName('noUi-origin')[3].setAttribute("disabled", "");
style(engineTempCtrl.getElementsByClassName('noUi-origin')[0]).display = "none";
style(engineTempCtrl.getElementsByClassName('noUi-origin')[2]).display = "none";
style(engineTempCtrl.getElementsByClassName('noUi-origin')[3]).display = "none";
style(engineTempCtrl.getElementsByClassName('noUi-connect')[0]).background = "blue";
style(engineTempCtrl.getElementsByClassName('noUi-connect')[1]).background = "orange";
style(engineTempCtrl.getElementsByClassName('noUi-connect')[2]).background = "red";
noUiSlider.create(engineOnCtrl,
    {
        range: { min: 0, max: 1 },
        step: 1,
        start: 0,
        behaviour: 'drag tap',
    });
sanderCtrl.noUiSlider = noUiSlider.create(sanderCtrl,
    {
        range: { min: 0, max: 1 },
        start: [0, 0],
        behaviour: 'tap hover',
        connect: [true, false, false],
    });
sanderCtrl.getElementsByClassName('noUi-origin')[0].setAttribute("disabled", "");
style(sanderCtrl.getElementsByClassName('noUi-origin')[0]).display = "none";

throttleCtrl.noUiSlider.on('set', values => actions.SetThrottle(Number(values[1])));
breakCtrl.noUiSlider.on('set', values => actions.SetBreak(Number(values[1])));
reverserCtrl.noUiSlider.on('set', values => actions.SetReverser(Number(values[0])));
engineOnCtrl.noUiSlider.on('set', values => 'SetEngineOn' in actions && actions.SetEngineOn(Number(values[0]) !== 0));
sanderCtrl.noUiSlider.on('set', setSander);
coupleBtn.addEventListener('click', () => actions.Couple(Number(couplerCtrl.noUiSlider.get())));
uncoupleBtn.addEventListener('click', () => actions.UnCouple(Number(couplerCtrl.noUiSlider.get())));
window.onunload = function () {
    if (clientId !== undefined) {
        if (navigator.sendBeacon(`unregister?${clientId}`)) return;
        const client = new XMLHttpRequest();
        client.open("GET", `unregister?${clientId}`, false); // third parameter indicates sync xhr
        client.send();
    }
};
targetSpeedChk.addEventListener('change', e => {
    if (!targetSpeedChk.checked) {
        speedCtrl.getElementsByClassName('noUi-origin')[1].setAttribute("disabled", "");
        speedCtrl.getElementsByClassName('noUi-origin')[2].setAttribute("disabled", "");
        style(speedCtrl.getElementsByClassName('noUi-origin')[1]).display = "none";
        style(speedCtrl.getElementsByClassName('noUi-origin')[2]).display = "none";
        autoSpeedMode = 0;
    } else {
        targetRangeChk.checked = false;
        speedCtrl.getElementsByClassName('noUi-origin')[1].removeAttribute("disabled");
        speedCtrl.getElementsByClassName('noUi-origin')[2].setAttribute("disabled", "");
        style(speedCtrl.getElementsByClassName('noUi-origin')[1]).display = null;
        style(speedCtrl.getElementsByClassName('noUi-origin')[2]).display = "none";
        autoSpeedMode = 1;
    }
});
targetRangeChk.addEventListener('change', e => {
    if (!targetRangeChk.checked) {
        speedCtrl.getElementsByClassName('noUi-origin')[1].setAttribute("disabled", "");
        speedCtrl.getElementsByClassName('noUi-origin')[2].setAttribute("disabled", "");
        style(speedCtrl.getElementsByClassName('noUi-origin')[1]).display = "none";
        style(speedCtrl.getElementsByClassName('noUi-origin')[2]).display = "none";
        autoSpeedMode = 0;
    } else {
        targetSpeedChk.checked = false;
        speedCtrl.getElementsByClassName('noUi-origin')[1].removeAttribute("disabled");
        speedCtrl.getElementsByClassName('noUi-origin')[2].removeAttribute("disabled");
        style(speedCtrl.getElementsByClassName('noUi-origin')[1]).display = null;
        style(speedCtrl.getElementsByClassName('noUi-origin')[2]).display = null;
        autoSpeedMode = 2;
    }
});
autoSanderChk.addEventListener('change', async e => {
    if (!('Sander' in state && 'SetSander' in actions)) return;
    if (autoSanderChk.indeterminate) {
        autoSanderChk.checked = false;
        return;
    }
    if (!autoSanderChk.checked) {
        autoSanderChk.indeterminate = true;
        return;
    }
    let val = state.Sander;
    if (val === 0) val = 1;
    actions.SetSander(0);

    while (autoSanderChk.checked) {
        if (!state.WheelSlip) while (! await stateChanged.WheelSlip);
        actions.SetSander(val);
        await delay(5000);
        if (state.WheelSlip) while (await stateChanged.WheelSlip);
        actions.SetSander(0);
        await delay(1000);
    }
    autoSanderChk.indeterminate = false;
});

async function send(cmd: string, data?: any) {
    if (clientId === undefined)
        await getClient;
    await fetch(`send?${clientId}`, { body: JSON.stringify({ Name: cmd, Json: data }), method: "POST" });
}

(async () => {
    if (clientId === undefined)
        await getClient;

    while (true) {
        const msg = await (await fetch(`poll?${clientId}`)).json();
        handleMessage(msg.Name, msg.Json);
    }
})();

/**
 * 
 * @param {string} command
 * @param data
 */
function handleMessage(command: string, data: any) {
    switch (command) {
        case "init-state":
            state = data as any; // just assume the data we get is correct
            initState();
            stateUpdated(data);
            break;
        case "update-state":
            state = Object.assign(state, data);
            stateUpdated(data);
            break;
        case "error":
            alert(data);
            break;
        case "action-list":

            const actionsObj: any = {};
            actionsObj.LocoType = state.LocoType;

            for (const action of data) {
                const name = action.name;
                const type = action.type;
                actionsObj[name] = (arg: any) => {
                    // todo: verify arg type

                    send("action", { name, data: arg });
                }
            }

            actions = actionsObj;

            break;
        case "paired":
            break;

        default:
            alert(`Unknown message: ${command}${data && ` with data:\n\n${data}`}`);
    }
}

function style(element: Element): CSSStyleDeclaration {
    if (element instanceof HTMLElement) return element.style;
    return {} as any;
}

function stateUpdated(patch: Partial<LocoStateSum>) {
    if (typeof state === 'undefined') return;

    stateContainer.innerText = JSON.stringify(state, null, 2);

    // todo: do this in a way that doesn't cause ts to explode or requires any casts
    for (const key in patch) {
        const element = (patch as any)[key];
        const resolve = key in stateChanged.resolve && (stateChanged.resolve as any)[key];
        (stateChanged as any)[key] = new Promise<any>(resolve => (stateChanged.resolve as any)[key] = resolve);
        if (resolve) resolve(element);
    }

    if ('LocoType' in patch) {
        document.body.className = patch.LocoType!;

    }

    // base
    if ('Throttle' in patch) throttleCtrl.noUiSlider.setHandle(0, patch.Throttle!, false);
    if ('TargetThrottle' in patch) throttleCtrl.noUiSlider.setHandle(1, patch.TargetThrottle!, false);
    if ('Break' in patch) breakCtrl.noUiSlider.setHandle(0, patch.Break!, false);
    if ('TargetBreak' in patch) breakCtrl.noUiSlider.setHandle(1, patch.TargetBreak!, false);
    if ('Reverser' in patch) reverserCtrl.noUiSlider.setHandle(0, patch.Reverser!, false);
    if ('MinCouplePos' in patch || 'MaxCouplePos' in patch)
        couplerCtrl.noUiSlider.updateOptions(
            { range: { min: state.MinCouplePos, max: state.MaxCouplePos } });
    if ('Speed' in patch) speedCtrl.noUiSlider.setHandle(0, patch.Speed!, false);
    if ('CanCouple' in patch) coupleBtn.disabled = !patch.CanCouple!;
    if ('WheelSlip' in patch) style(sanderCtrl.getElementsByClassName("noUi-handle")[1]).backgroundColor = patch.WheelSlip! ? "red" : null;

    // shunter
    if ('EngineTemp' in patch) engineTempCtrl.noUiSlider.setHandle(1, patch.EngineTemp!, false);
    if ('EngineOn' in patch) engineOnCtrl.noUiSlider.setHandle(0, patch.EngineOn! ? 1 : 0, false);
    if ('Sander' in patch) sanderCtrl.noUiSlider.setHandle(1, patch.Sander!, false);
    if ('SanderFlow' in patch) sanderCtrl.noUiSlider.setHandle(0, patch.SanderFlow!, false);

    if (!state) return;
    if (typeof state.UpdateNum === 'undefined') state.UpdateNum = 0;
    state.UpdateNum++;
}

function setSander(values: string[]) {
    let val = Number(values[1]);
    if (state.LocoType === "shunter") sanderCtrl.noUiSlider.setHandle(1, val = val > 0.5 ? 1 : 0, false);
    if ('Sander' in state && 'SetSander' in actions && val !== state.Sander) actions.SetSander(val);
}

function initState() {
    stateChanged = { resolve: {} } as any;
}

async function autoSpeedTest() {
    while (true) {
        await delay(1000);

        if (autoSpeedMode === 0) continue;

        let min = 0;
        let max = 120;
        let current = state.Speed * Math.sign(state.Reverser); // TODO: actual direction
        let diff = 0;

        if (autoSpeedMode === 1) {
            min = max = Number(speedCtrl.noUiSlider.get()[1]) * Math.sign(state.Reverser);
        } else {
            min = Number(speedCtrl.noUiSlider.get()[1]) * Math.sign(state.Reverser);
            max = Number(speedCtrl.noUiSlider.get()[2]) * Math.sign(state.Reverser);
        }

        if (min > max) {
            let tmp = min;
            min = max;
            max = tmp;
        }

        if (current < min) {
            diff = min - current;
        } else if (current > max) {
            diff = max - current;
        }

        // todo: better reactions
        let delta = Math.sign(diff) * Math.sqrt(Math.abs(diff)) / 100;

        if (delta === 0) continue;

        if (state.TargetThrottle !== 0 && state.TargetBreak !== 0) {
            if (delta > 0) {
                if (delta > Number(breakCtrl.noUiSlider.get()[1])) {
                    delta -= Number(breakCtrl.noUiSlider.get()[1]);
                    breakCtrl.noUiSlider.setHandle(1, 0, true);
                } else {
                    breakCtrl.noUiSlider.setHandle(1, Number(breakCtrl.noUiSlider.get()[1]) - delta, true);
                    delta = 0;
                }
            } else {
                if (delta > Number(throttleCtrl.noUiSlider.get()[1])) {
                    delta -= Number(throttleCtrl.noUiSlider.get()[1]);
                    throttleCtrl.noUiSlider.setHandle(1, 0, true);
                } else {
                    throttleCtrl.noUiSlider.setHandle(1, Number(throttleCtrl.noUiSlider.get()[1]) - delta, true);
                    delta = 0;
                }
            }
        }

        let power = Number(throttleCtrl.noUiSlider.get()[1]) - Number(breakCtrl.noUiSlider.get()[1]);

        power += delta;

        if (power > 0) {
            throttleCtrl.noUiSlider.setHandle(1, power, true);
            breakCtrl.noUiSlider.setHandle(1, 0, true);
        } else {
            throttleCtrl.noUiSlider.setHandle(1, 0, true);
            breakCtrl.noUiSlider.setHandle(1, -power, true);
        }
    }

}

autoSpeedTest();

/**
 * Toggle fullscreen function who work with webkit and firefox.
 * https://gist.github.com/demonixis/5188326
 * @function toggleFullscreen
 * @param {Object} event
 */
function toggleFullscreen(event?: any) {
    let doc: any = document;
    let element: any = document.documentElement;

    if (event instanceof HTMLElement) {
        element = event;
    }

    const isFullscreen = doc.webkitIsFullScreen || doc.mozFullScreen || false;

    element.requestFullscreen = element.requestFullscreen ||
        element.requestFullScreen ||
        element.webkitRequestFullScreen ||
        element.mozRequestFullScreen ||
        function () { return false; };
    doc.cancelFullscreen = doc.cancelFullscreen ||
        doc.webkitCancelFullScreen ||
        doc.mozCancelFullScreen ||
        function () { return false; };

    isFullscreen ? doc.cancelFullscreen() : element.requestFullscreen();
}