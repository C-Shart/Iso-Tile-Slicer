# General Isometric Tile Slicer
This is a command line program to take an image and slice it up into isometric tiles.

It was originally designed for standard 44x44 Ultima Online tiles. I found myself needing something to slice images into isometric tiles and this was the best thing I could find, so I decided to start modifying for my current and future needs.

## How To Use
1. Download the [latest release](https://github.com/C-Shart/Iso-Tile-Slicer/releases/latest)

    a. Alternately, you can download the source code and run `dotnet build` in the terminal.

2. Will update this more fully later.

2. Next, put it somewhere, open a command prompt, terminal, powershell, etc and type `IsoTileSlicer.exe --image imagepath/mypic.png` (or `dotnet IsoTileSlicer.dll  --image imagepath/mypic.png for unix`)

3. Wait for it to complete. There will be a few useless empty tiles, this is normal and could be fixed if someone was really bothered about it.

By default they will be in a folder named `out`. There is also a layout.html file to help visualize the layout.  

## Other options??
There are a few, play with them if desired, I can't promise the outcome will be desired.

`--tilesize 44`  <- Tile size.  
`--offset 1`  <- 0 if tile size is an odd number, 1 if it is an even number. Other numbers have unknown results.  
`--output out`  <- Where to save sliced images to.  
`--filename {0}` <- {0} is the image number, you can use tile{0} or file{0} for example.  
`--startingnumber 0` <- The file number to start with.  

# Screenshots!! I won't use it without screenshots.

![image](https://github.com/bittiez/UO-Iso-Tile-Slicer/assets/3859393/a2c1c3ca-bf57-4bb4-877a-6baf4d232175)

Into

![image](https://github.com/bittiez/UO-Iso-Tile-Slicer/assets/3859393/5eae588c-3b95-47cd-a7ac-b658c7993bb9)

