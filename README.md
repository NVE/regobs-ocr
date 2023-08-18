# Regobs OCR

## OCR of snow profile layers and temperature profile

* Hand-written sample profiles are available in [samples/full-profiles/](./samples/full-profiles/).
  * These profiles are fetched from Regobs, and you can find the original observation by going to `https://plot.regobs.no/v1/SnowProfile/123456/SimpleProfile`, where you replace `123456` by the profile's ID number. There are some slight differences between the hand-written samples and the Regobs observations. Some are there by mistake and some were changed on technical grounds.
* The JSON-samples in the same directory are structured according to the TypeScript interface `SnowProfile` defined in [ts/snow_profile_t.ts](./ts/snow_profile_t.ts).

## Recognition of snow grain classification symbols

* The symbols are defined in Appendix A of [The International Classification for Seasonal Snow on the Ground](https://unesdoc.unesco.org/ark:/48223/pf0000186462/PDF/186462eng.pdf.multi).
* Hand-written samples for the most important symbols are available in [samples/grain-classes/](./samples/grain-classes/).


## Valid inputs for hand-written snow profile fields

### Thickness of layer

*This field is mandatory.*

Must be a number greater than 0 with at most 1 decimal place. Decimal separator is a comma.

#### Examples

* 1
* 12,5
* 0,5

### Liquid water contents

The following are the basic codes for liquid water content:

* D
* M
* W
* V
* S

These may be combined with neighbours to denote water contents between the different classes:

* D-M
* M-W
* W-V
* V-S

### Layer hardness

*This field is mandatory.*

The following are the basic codes for layer hardness:

* F
* 4F
* 1F
* P
* K
* I

Each class may be suffixed by + or -:

* F-
* F+
* 4F-
* 4F+
* 1F-
* 1F+
* P-
* P+
* K-
* K+
* I-
* I+

The midpoint between classes can be expressed in the same way as with liquid water content:

* F-4F
* 4F-1F
* 1F-P
* P-K
* K-I

A hardness gradient between the top and bottom of the snow layer can be expressed by specifying two hardnesses (not necessarily neighbours), where the first hardness refers to the top of the layer, and the second to the bottom, e.g.:

* F/4F
* 4F/F
* F/I
* 4F+/1F-P

### Grain shape classification

The codes for the main and subclasses of grain shapes are:

* PP
  * PPco
  * PPnd
  * PPpl
  * PPsd
  * PPir
  * PPgp
  * PPhl
  * PPip
  * PPrm
* MM
  * MMrp
  * MMci
* DF
  * DFdc
  * DFbk
* RG
  * RGsr
  * RGlr
  * RGwp
  * RGxf
* FC
  * FCso
  * FCsf
  * FCxr
* DH
  * DHcp
  * DHpr
  * DHch
  * DHla
  * DHxr
* SH
  * SHsu
  * SHcv
  * SHxr
* MF
  * MFcl
  * MFpc
  * MFsl
  * MFcr
* IF
  * IFil
  * IFic
  * IFbi
  * IFrc
  * IFsc

The observer may choose to specify a primary and secondary grain. The secondary grain class is then appended inside parentheses, e.g.:

* PP(DF)
* FC(FCxr)
* PP(PPgp)
* MFpc(FC)
* SHxr(DF)
* MF(RG)

### Grain size

Must be a number greater than 0 with at most 1 decimal place. Decimal separator is a comma.

A range may be specified by stating the minimum size followed by the maximum separated by a hyphen.

#### Examples

* 1
* 12,5
* 0,5
* 0,1-0,3

### Air temperature

Must be a number with at most 1 decimal place. Decimal separator is a comma.

#### Examples

* 1
* -5
* 12,5
* -0,5
* 0

### Snowpack temperature

Must be a number less than or equal to 0 with at most 1 decimal place. Decimal separator is a comma.

#### Examples

* 0
* -0,1
* -12
* -7,5

### Snowpack temperature depth

Must be an integer greater than 0 (since the snow surface depth is pre-filled).

#### Examples

* 1
* 5
* 10
* 58