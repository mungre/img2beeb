# img2beeb

Convert animated gifs into BBC Micro palette animations.

This is only of any use when the animated gif was created from a BBC Micro palette animation in the first place.

The code is written in C# using VS2010, but a batch file is also provided to build the application in the absence of Visual Studio.

## Conversion

Open a command prompt.  Type

    git clone https://github.com/mungre/img2beeb
    cd img2beeb
    build

Copy animated gifs into the gifs subdirectory.  Execute

    build

for a second time.  The mode2 directory should contain the results.

## Animation

The BBC BASIC program `basic\ani.txt` displays a menu of all "P.*" files and animates the selected one.

The [SSDs posted on stardot](https://stardot.org.uk/forums/viewtopic.php?f=11&t=13294&start=210#p198503) illustrate this.
