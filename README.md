# img2beeb

Convert animated gifs into BBC Micro palette animations.

This is only of any use when the animated gif was created from a BBC Micro palette animation in the first place.

The code is written in C# using .NET Core and SixLabors.ImageSharp.  It has been tested on Windows but should also work on Linux and Mac.

## Conversion

Ensure that you have [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) installed.

Open a command prompt.  Type

    git clone https://github.com/mungre/img2beeb
    cd img2beeb
    dotnet build
    mkdir gifs
    mkdir mode2

Copy animated gifs into the gifs subdirectory.  Execute

    dotnet run

The mode2 directory should contain the results.

## Animation

The BBC BASIC program `basic\ani.txt` displays a menu of all "P.*" files and animates the selected one.

The [SSDs posted on stardot](https://stardot.org.uk/forums/viewtopic.php?f=11&t=13294&start=210#p198503) illustrate this.
