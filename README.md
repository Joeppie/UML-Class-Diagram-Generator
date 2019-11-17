# UML-Class-Diagram-Generator
Generates class diagrams from doxygen XML with callgraphs generated.

Currently work in progress

## Usage
To use this code, use the supplied Doxyfile on source code with doxygen and point the program.cs Main method to the generated xml folder
Doxygen and Graphviz (the latter must be manually added to Path variable) are required.

Steps:

1. Install doxygen
2. Install graphviz
3. Add Graphviz to Path variable in windows (so that opening a cmd.exe window and typing dot launches that program)
4. Run doxygen on the folder containing the DoxyFile (for example in this repository itself)
5. Modify the program.cs to point to the folder with the xml output generated (relative to doxyfile this is ./doxygen/xml)

## Example result.

This code is still a work in progress..

![Example class diagram generated from this repository](https://raw.githubusercontent.com/Joeppie/UML-Class-Diagram-Generator/master/TestImage.svg?sanitize=true)

## License

GNU Affero for now.
Use this work for whatever purpose you may want, as long as you redistribute modified or derived sources of the code, as per the GNU Affero license. https://en.wikipedia.org/wiki/Affero_General_Public_License.
