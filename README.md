# UML-Class-Diagram-Generator
Generates class diagrams from doxygen XML with callgraphs generated.
Currently work in progress.

## Purpose
Doxygen is a great tool, but it is lackluster in terms of class diagrams.

Adding these can be done by some tools, for example using [doxygraph](https://github.com/jitsuCM/doxygraph) (non-cannonical link).
However, Doxygraph is written in Perl and tricky to use properly.

This UML-Class-Diagram-Generator should be able to output a class diagram without much hassle, provided you can run .NET on your platform

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

Yes, this diagram is the source code of this repository itself, represented as a class diagram. I should add interfaces, to demonstrate how these are correctly displayed *(Doxygen currently incorrectly displays implementations of interfaces as inheritance from the interface.)

Sadly, not all uses of classes/interfaces (dependency and uses) are correctly recognized; this seems to be a bug in Doxygen's C# support, as they are missing from the XML this tool bases on, but has not yet been reported.

## License

GNU Affero for now.
Use this work for whatever purpose you may want, as long as you redistribute modified or derived sources of the code, as per the GNU Affero license. https://en.wikipedia.org/wiki/Affero_General_Public_License.
