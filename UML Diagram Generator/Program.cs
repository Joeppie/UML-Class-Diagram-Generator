using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace UML_Diagram_Generator
{
    static class Extensions
    {

        public static string Escape(this string s)
        {
            return SecurityElement.Escape(s);
        }
        /// <summary>Returns a dot syntax safe version of a name to avoid problems.</summary>
        public static string Safe(this string s)
        {
            return (s ?? "").Escape().Replace(":", "_").Replace("-", "_").Replace(">", "_");
        }

        /// <summary>Keep just the name.</summary>
        public static string NoNameSpaces(this string s)
        {
            return (s ?? "").Escape().Split(new string[] { "::" }, StringSplitOptions.None).Last();
        }

        public static string WithoutGuid(this string s)
        {
            return s.Substring(0, s.Length - 35); //Remove the trailing 35 characters containing respectively a _ anda  34 character GUID.
        }
    }


    class Program
    {
        /// <summary>
        /// Call the program either with or without arguments specifying in which folder it should run.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //debug code

            /*  ClassNode test = new ClassNode {
                  Attributes = new List<Attribute> { new Attribute { Name = "Size", Type = "int", Public = true } },
                  Methods = new List<Method> { new Method { Name = "GetSize", Type = "int", Public = true } },
                  Name = "Maat"
              };

              String template = $@"  digraph G {{
              fontname = ""Bitstream Vera Sans""
              fontsize = 8

              node [
                      fontname = ""Bitstream Vera Sans""
                      fontsize = 8
                      shape = ""record""
              ]

              edge [
                      fontname = ""Bitstream Vera Sans""
                      fontsize = 8
              ]
              {test.ToString()}
  }}
  ";

              FileDotEngine.Run(template, "joeptest.svg");

              FileDotEngine.Run(@"

  digraph G {
          fontname = ""Bitstream Vera Sans""
          fontsize = 8

          node [
                  fontname = ""Bitstream Vera Sans""
                  fontsize = 8
                  shape = ""record""
          ]

          edge [
                  fontname = ""Bitstream Vera Sans""
                  fontsize = 8
          ]

          Animal [
                  label = ""{Animal|+ name : string\l+ age : int\l|+ die() : void\l}""
          ]

          subgraph clusterAnimalImpl {
                  label = ""Package animal.impl""

                  Dog [
                          label = ""{Dog||+ bark() : void\l}""
                  ]

                  Cat [
                          label = ""{Cat||+ meow() : void\l}""
                  ]
          }

          edge [
                  arrowhead = ""empty""
          ]

          Dog -> Animal
          Cat -> Animal

          edge [
                  arrowhead = ""none""

                  headlabel = ""0..*""
                  taillabel = ""0..*""
          ]

          Dog -> Cat
  }


  ", "test.svg");*/



            string diagramName = "ClassDiagram";
            
             var parsed = ParseDoxygenStructure(@"..\..\doxygen\xml");
             //var parsed = ParseDoxygenStructure(@"C:\Users\Joep\source\repos\DatabasesTentamenCheckerConsoleApp\doxygen\xml");
             StringBuilder result = new StringBuilder();

            foreach (var item in parsed)
            {
                item.OutputSelf(result);
                result.AppendLine();
            }

            foreach (var item in parsed.Where(p=>p is Node))
            {
                (item as Node).OutputRelations(result);
                result.AppendLine();
            }

            String template = $@"  digraph {diagramName} {{
              fontname = ""Consolas""
              fontsize = 8

              node [
                      fontname = ""Consolas""
                      fontsize = 8
                      shape = ""record""
              ]

              edge [
                      fontname = ""Consolas""
                      fontsize = 8
              ]
              {result.ToString()}
  }}
  ";
            FileDotEngine.Run(template ,"joeptest.svg");



            /*            if(args.Length<1)
                        {
                            if(!Directory.Exists(args[0]))
                            {
                                throw new ArgumentException("specified directory does not exist.");
                            }
                            Directory.SetCurrentDirectory(args[0]);
                        }
                        ParseDoxygenStructure(".");*/

        }



        static List<UMLEntity> ParseDoxygenStructure(string folder)
        {
            XDocument doc = XDocument.Load(Path.Combine(folder,"index.xml"));

            var relevant = new HashSet<string>  { "class", "interface" };

            var namespaces = doc.Descendants("compound").Where(c => c.Attribute("kind").Value.ToLower() == "namespace" );

            //Redelijk veel werk; todo voor later.
            /*          Dictionary<string,Package> packages = new Dictionary<string,Package>();

                      foreach (var n in namespaces)
                      {
                          string[] hierarchy = n.Element("name").Value.Split(new string[] { "xx" },StringSplitOptions.None);

                          bool first = true;
                          foreach (var item in hierarchy)
                          {
                              if(first)
                              {
                                  if(!packages.ContainsKey(item))
                                  {
                                      packages[item] = new Package() { Name = item };
                                  }
                              }
                              else
                              {

                              }
                          }

                      }
            */

            List<UMLEntity> umlEntities = new List<UMLEntity>();

            var elementsToMap = doc.Descendants("compound").Where(c => relevant.Contains(c.Attribute("kind").Value.ToLowerInvariant()));

            foreach (var element in elementsToMap)
            {
                string nodeDocUrl = Path.Combine(folder, element.Attribute("refid").Value + ".xml");
                XDocument nodeDoc = XDocument.Load(nodeDocUrl);

                //Create a node of the correct type.
                Node node;
                string elementType = element.Attribute("kind").Value.ToLowerInvariant();
                switch (elementType)
                {
                    case "interface":
                        node = new InterfaceNode();
                        break;
                    case "class":
                        node = new ClassNode();
                        break;
                    default: throw new NotImplementedException($"No implementation exists for a node of type {elementType}, encountered in {nodeDocUrl}");
                }
                umlEntities.Add(node);

                node.Name = element.Element("name").Value;

                //Find directly inherited node.

                // inheritancegraph.node[@id = inheritancegraph.node[@id = 1].childnode@refId]

                var inheritance = nodeDoc.Descendants("inheritancegraph").FirstOrDefault();

                if(inheritance != null)
                { 
                    foreach (var inheritee in inheritance.Descendants("node").First(n => n.Attribute("id").Value == "1").Elements("childnode").Select(c=>c.Attribute("refid").Value))
                    {
                        string name = inheritance.Descendants("node").First(n => n.Attribute("id").Value == inheritee).Element("label").Value;
                        node.Inherits.Add(new Usage { TypeName = name });
                    }
                }





                //Populate attributes and methods.
                foreach (var member in nodeDoc.Descendants("memberdef"))
                {
                    string memberType = member.Attribute("kind").Value.ToLowerInvariant();
                    switch (memberType)
                    {
                        case "function":
                            node.Methods.Add(new Method
                            {
                                Name = member.Element("name").Value,
                                Public = member.Attribute("prot").Value.ToLowerInvariant() == "public",
                                Type = member.Element("type").Value
                            });

                            //Check what the function's code references and then produce dependencies or associations for it.
                            //e.g.class_database_examination_1_1_examination_1a65be916fcc856e6723cd6ae471b1b51a means that class_database_examination_1_1_examination is used.

                            //Todo: perhaps all the code should use the reference Ids that are used by doxygen? this will prevent name collissions between namespaces.
                            var referencedClassesIds = member.Descendants("references").Select(e => e.Attribute("refid").Value.WithoutGuid()).ToList();

                            var referencedCompounds = doc.Descendants("compound").Where(c => referencedClassesIds.Contains(c.Attribute("refid").Value)).ToList();

                            foreach (var item in referencedCompounds)
                            {
                                var usage = new Usage { Public = false, TypeName = item.Element("name").Value.NoNameSpaces() };

                                //Avoid duplication, and don't self-reference for 'dependency' or 'association'; that is superfluous.
                                if (!node.Uses.Any(u => u.TypeName == usage.TypeName && u.Public == usage.Public) && usage.TypeName != node.Name.NoNameSpaces())
                                { 
                                    node.Uses.Add(usage);
                                }

                                ;
                                ///   if(item.)
                            }
                            

                            break;
                        case "property":
                        case "variable":

                            //Check cardinality
                            //Also, usages may not just be of a 'string' or built in type, but of types on the class diagram being printed.
                            //In that case, make UML links, not mentions of attributes; we cannot find that out yet.

                            //Annoying piece of XML: <type>List&lt; <ref refid="class_database_examination_1_1_assertion" kindref="compound">Assertion</ref> &gt;</type>


                            //bool compound = member.Element("type").Attribute("kindref") != null && member.Element("type").Attribute("kindref").Value.ToLowerInvariant() == "compound";
                            bool compound = member.Element("type").Element("ref") != null &&
                            member.Element("type").Element("ref").Attribute("kindref").Value.ToLowerInvariant() == "compound";
                            string typeName = compound ? member.Element("type").Element("ref").Value : member.Element("type").Value;

                            var use = new Usage { TypeName = typeName, Compound = compound, DeclaredName = member.Element("name").Value };

                            node.Uses.Add(use);
                                  
                            break;
                        default: throw new NotImplementedException($"No implementation exists for a member of type {memberType}, encountered in {nodeDocUrl}");
                    }
                }



            }

            //Resolve uses by classes and interfaces.
            var nodes = umlEntities.Select(n => n as Node).Where(n => n != null);
            Dictionary<string, Node> lookup = nodes.ToDictionary(k => k.Name.NoNameSpaces(), v => v);

            foreach (var node in nodes)
            {
                node.ResolveUses(lookup);
            }

            

            return umlEntities;
        }



        /// <summary>
        /// Represents a UML entity that can be represented.
        /// </summary>
        abstract class UMLEntity
        {
            public string Name { get; set; }


            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                OutputSelf(builder);
                return builder.ToString();
            }

            /// <summary>
            /// Output all that is required to render this UML element within some diagram.
            /// </summary>
            /// <param name="builder">The stringbuilder to render to</param>
            /// <returns></returns>
            public abstract StringBuilder OutputSelf(StringBuilder builder);

        }


        class Package : UMLEntity
        {
            public string Name { get; private set; }
            public List<Node> Nodes { get; private set; }
            public List<Package> Packages { get; private set; }



            public Package()
            {
                Nodes = new List<Node>();
                Packages = new List<Package>();
            }

            public override StringBuilder OutputSelf(StringBuilder builder)
            {
                throw new NotImplementedException();
            }

        }

        class Attribute : UMLEntity
        {
            public string Type { get; set; }
            public bool Public { get; set; }

            public override StringBuilder OutputSelf(StringBuilder builder)
            {
                builder.Append(Public ? "+" : "-");
                builder.Append(" ");
                builder.Append(Name.NoNameSpaces());
                builder.Append(" : ");
                builder.Append(Type.Escape());
                builder.Append(@"\l");
                return builder;
            }
        }

        class Method : UMLEntity
        {
            public bool Public { get; set; }
            public string Type { get; set; }

            public override StringBuilder OutputSelf(StringBuilder builder)
            {
                builder.Append(Public ? "+" : "-");
                builder.Append(" ");
                builder.Append(Name.NoNameSpaces());
                builder.Append("() : ");
                builder.Append(Type.Escape());
                builder.Append(@"\l");
                return builder;
            }
        }


        class Usage<T> where T : Node
        {
            /// <summary>True when there may be more than one element.</summary>
            public bool Compound { get; set; }

            public T node { get; set; }
            /// <summary>Whether or not the usage is properly resolved! </summary>
            public bool Resolved => node != null;

            /// <summary>Whether or not to actually display the use; false when it remains an attribute i.e. doesn't actually map a displayable relationship to another UmlEntity.</summary>
            /// <note>This is true when resolving failed.</note>
            public bool Relevant { get; set; }

            public bool Public { get; set; }

            /// <summary>The name the attribute/property has in code.</summary>
            public string DeclaredName { get; set; }
            
            private string _typeName;
            /// <summary>The name of the type of the attribute/property.</summary>
            public string TypeName
            {
                get { return node != null ? node.Name : _typeName; }
                set 
                {
                    if (node != null) throw new InvalidOperationException("Cant set name of resolved Usage.");
                    _typeName = value;
                }
            }

        }
        class Usage : Usage<Node> { }
        class InterfaceUsage : Usage<InterfaceNode> { }
        class ClassUsage : Usage<ClassNode> { }

        abstract class Node : UMLEntity
        {
            public Package Package { get; set; }
            public List<Usage> Inherits { get; set; }
            public List<Usage> Uses { get; set; }
            public List<Attribute> Attributes { get; set; }
            public List<Method> Methods { get; set; }


            public bool IsInterface { get; set; }

            /// <summary>
            /// Outputs a list of the relations that this class has toward other classes.
            /// </summary>
            /// <param name="builder">the stringbuilder to render to</param>
            /// <returns></returns>
            public StringBuilder OutputRelations(StringBuilder builder)
            {

                foreach (var use in Uses.Where(u=>u.Relevant))
                {
                    string arrowhead;
                    if (use.Compound)
                    {
                        arrowhead = "arrowhead=odiamond";
                        builder.AppendLine($@"""{ use.TypeName.NoNameSpaces()}""->""{Name.NoNameSpaces()}"" [{arrowhead}  {(use.node is InterfaceNode ? "style =dashed" : "")}  label=""{use.DeclaredName.NoNameSpaces()}"" ]");

                    }
                    else
                    {
                        arrowhead = "arrowhead = normal";
                        builder.AppendLine($@"""{ Name.NoNameSpaces()}""->""{use.TypeName.NoNameSpaces()}"" [{arrowhead}  {(use.node is InterfaceNode ? "style =dashed" : "")}  label=""{use.DeclaredName.NoNameSpaces()}"" ]");

                    }

                    //Create the actual arrow from this node to the used one, if the used node is an interface, make the line dashed to indicate dependency
                }


                foreach (var inheritee in Inherits)
                {
                    builder.AppendLine(@" edge [arrowhead = ""empty"" ]"); 
                    builder.AppendLine($"\"{Name.NoNameSpaces()}\"->\"{inheritee.TypeName.NoNameSpaces()}\" [{ (inheritee.node is InterfaceNode ? "style=dashed" : "style=solid")} { (inheritee==null ? ", label=\"??\"" : " " ) }  ]");
                }
                
                return builder;
            }


            public Node()
            {
                Inherits = new List<Usage>();
                Uses = new List<Usage>();
                Attributes = new List<Attribute>();
                Methods = new List<Method>();
            }

            public void ResolveUses(Dictionary<string,Node> lookup)
            {
                foreach (var use in Uses)
                {
                    if (!use.Resolved)
                    {
                        if (lookup.ContainsKey(use.TypeName))
                        {
                            use.Relevant = true;
                            use.node = lookup[use.TypeName];
                        }
                        else //Not part of UML diagram itself, mark it as an attribute.
                        {
                            use.Relevant = false;
                            Attributes.Add(new Attribute
                            {
                                Name = use.DeclaredName,
                                Public = use.Public,
                                Type = use.TypeName
                            });
                        }
                    }
                }

                foreach (var inheritee in Inherits)
                {
                    if (!inheritee.Resolved)
                    {
                        if (lookup.ContainsKey(inheritee.TypeName))
                        {
                            inheritee.Relevant = true;
                            inheritee.node = lookup[inheritee.TypeName];
                        }
                        else //Not part of UML diagram itself, mark it as an attribute.
                        {
                           //Not sure what to make of it, it's not in the diagram.
                        }
                    }
                }
            }

            public override StringBuilder OutputSelf(StringBuilder builder)
            {

                //Todo: markering toevoegen <<interface>> boven de naam van de klasse zelf.
                builder.Append("\"");
                builder.Append(Name.NoNameSpaces());
                builder.Append("\"");
                builder.Append(" [ label=\"{");
                builder.Append(Name.NoNameSpaces());

                builder.Append("|");
                foreach (var attribute in Attributes)
                {
                    attribute.OutputSelf(builder);
                }
                builder.Append("|");
                foreach (var method in Methods)
                {
                    method.OutputSelf(builder);
                }
                builder.Append("}\"");

                builder.Append(" ] ");
                return builder;
            }
        }

        class ClassNode : Node
        {
            public ClassNode()
            {
                IsInterface = false;
            }
        }

        class InterfaceNode : Node
        {
            public InterfaceNode()
            {
                IsInterface = true;
            }
        }


        //Taken and adapted from codeproject: https://www.codeproject.com/Articles/1164156/Using-Graphviz-in-your-project-to-create-graphs-fr
        public static class FileDotEngine
        {
            public static void Run(string dot, string fileName)
            {
                string executable = @"dot.exe";
                File.WriteAllText(fileName, dot);

                System.Diagnostics.Process process = new System.Diagnostics.Process();

                // Stop the process from opening a new window
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                // Setup executable and parameters
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = string.Format(@"{0} -Tsvg -O", fileName);

                // Go
                process.Start();
                // and wait dot.exe to complete and exit
                process.WaitForExit();
            }
        }


    }
}
