---
hide:
    - toc
---
Maps a VIN's first 8 characters, the WMI and the VDS, to the model they point to.
 Each host loads its own rows, and these packages ship none.

| Property | Summary |
|----------|---------|
| WMI <div><strong>``string``</strong></div> | The World Manufacturer Identifier: VIN characters 1 to 3. |
| VDS <div><strong>``string``</strong></div> | The Vehicle Descriptor Section: VIN characters 4 to 8. |
| Katashiki <div><strong>``string``</strong></div> | The base Katashiki these characters point to: the code before the hyphen. Empty when they do not settle on one code. |
| ModelDescription <div><strong>``string``</strong></div> | The model name these characters point to. |
| Source <div><strong>``string``</strong></div> | Where the host's row came from. Not shown to anyone; it is there for whoever maintains the rows. |
