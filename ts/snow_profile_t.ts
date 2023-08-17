interface SnowProfile {
    layers: ProfileLayer[]
    airTemp?: number | null,
    snowTemp?: ProfileTemperature[] | null
}

interface ProfileLayer {
    thickness: number,
    hardness: Hardness,
    grain?: Grain | null,
    size?: number | [number, number] | null,
    lwc?: LWC | null
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
