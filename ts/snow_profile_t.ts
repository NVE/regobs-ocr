interface SnowProfile {
    layers: ProfileLayer[]
    airTemp?: number,
    snowTemp?: ProfileTemperature[]
}

interface ProfileLayer {
    thickness: number,
    hardness: Hardness,
    grain?: Grain,
    size?: number,
    lwc?: LWC
}

type LWC =
    "D" |
    "D-M" |
    "M" |
    "M-W" |
    "W" |
    "W-V" |
    "V" |
    "V-S" |
    "S";

type Grain = Grain_ | `${Grain_}(${Grain_})`;
type Grain_ =
  "PP" |
  "PPco" |
  "PPnd" |
  "PPpl" |
  "PPsd" |
  "PPir" |
  "PPgp" |
  "PPhl" |
  "PPip" |
  "PPrm" |
  "MM" |
  "MMrp" |
  "MMci" |
  "DF" |
  "DFdc" |
  "DFbk" |
  "RG" |
  "RGsr" |
  "RGlr" |
  "RGwp" |
  "RGxf" |
  "FC" |
  "FCso" |
  "FCsf" |
  "FCxr" |
  "DH" |
  "DHcp" |
  "DHpr" |
  "DHch" |
  "DHla" |
  "DHxr" |
  "SH" |
  "SHsu" |
  "SHcv" |
  "SHxr" |
  "MF" |
  "MFcl" |
  "MFpc" |
  "MFsl" |
  "MFcr" |
  "IF" |
  "IFil" |
  "IFic" |
  "IFbi" |
  "IFrc" |
  "IFsc";

type Hardness = Hardness_ | `${Hardness_}/${Hardness_}`;
type Hardness_ =
  "F-" |
  "F" |
  "F+" |
  "F-4F" |
  "4F-" |
  "4F" |
  "4F+" |
  "4F-1F" |
  "1F-" |
  "1F" |
  "1F+" |
  "1F-P" |
  "P-" |
  "P" |
  "P+" |
  "P-K" |
  "K-" |
  "K" |
  "K+" |
  "K-I" |
  "I-" |
  "I" |
  "I+";

interface ProfileTemperature {
    depth: number,
    temp: number
}

let profile319433: SnowProfile = {
    layers: [
        {
            thickness: 13.5,
            hardness: "P",
            grain: "RGwp",
            size: 0.1,
            lwc: "D"
        },
        {
            thickness: 18,
            hardness: "1F/4F-1F",
            grain: "PP(DF)",
            size: 0.3,
            lwc: "D"
        },
        {
            thickness: 66,
            hardness: "P",
            grain: "RG",
            size: 0.1,
            lwc: "D"
        },
        {
            thickness: 11,
            hardness: "4F",
            grain: "DH",
            size: 1.5,
            lwc: "D"
        }
    ],
    airTemp: -2,
    snowTemp: [
        {depth: 1, temp: -3.5},
        {depth: 10, temp: -4.5},
        {depth: 20, temp: -4.8},
        {depth: 30, temp: -4.8},
        {depth: 40, temp: -4.3},
        {depth: 50, temp: -3.8},
        {depth: 70, temp: -2.8},
        {depth: 90, temp: -1.5}
    ]
};