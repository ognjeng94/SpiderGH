using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Rhino;
using Rhino.Collections;
using Rhino.Input.Custom;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;
using Rhino.Geometry.Intersect;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.Xml.Linq;
using Grasshopper.Kernel.Types.Transforms;

namespace SpiderGH
{
    public class S_Chain
    {
        #region PROPERTIES


        public int Index { get; set; } //metadata
        public Guid ID { get; set; }
        public bool IsValid { get; set; }
        public int SimulationPhase { get; set; } //-1 invalid, 0 pre-processor, 1 processor, 2 post-processor

        public LineCurve InitialLineCurve { get; set; } //geometry

        public double Elasticity { get; set; } //chain data
        public double MaxLength { get; set; }
        public double PolyLength { get; set; }
        public double MassPerM { get; set; }

        public double NodeResolution { get; set; } //node data

        public List<S_Node> Nodes { get; set; } //connectivity

        #endregion

        #region CONSTRUCTOR

        ///this constructor will be used only to pass the data
        public S_Chain(Line line, double elasticity, double massPerM, double nodeRes)
        {
            Index = -1;
            ID = Guid.Empty;
            IsValid = true;
            SimulationPhase = 0;

            if (line.IsValid && Math.Abs(line.Length) > 1e-12)
            {
                InitialLineCurve = new LineCurve(line);
            }
            else
            {
                InitialLineCurve = null;
                IsValid = false;
            }

            if (elasticity < 0.01)
                elasticity = 0.01;
            if (elasticity > 10)
                elasticity = 10;

            Elasticity = elasticity;
            MaxLength = -1;
            PolyLength = -1;

            if (massPerM < 0)
                massPerM = Math.Abs(massPerM);

            MassPerM = massPerM;

            if (nodeRes < 0)
                nodeRes = Math.Abs(nodeRes);

            NodeResolution = nodeRes;

            Nodes = null;
        }

        /// <summary>
        /// This is a main constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="inputChain"></param>
        /// <param name="inputAnchors"></param>
        /// <param name="inputAnchorPts"></param>
        /// <param name="allNodes"></param>
        /// <param name="tolerance"></param>
        /// <param name="newGlobalNodes"></param>
        public S_Chain(int index, S_Chain inputChain, List<S_Anchor> inputAnchors, Point3dList inputAnchorPts,
                        List<S_Node> allNodes, double tolerance, out List<S_Node> newGlobalNodes)
        {
            #region 01 - PROPERTIES

            Index = index;
            ID = Guid.NewGuid();
            IsValid = true;
            SimulationPhase = 1;

            InitialLineCurve = new LineCurve(inputChain.InitialLineCurve.Line);

            Elasticity = inputChain.Elasticity;
            PolyLength = InitialLineCurve.Line.Length;
            MaxLength = InitialLineCurve.Line.Length / Elasticity;
            MassPerM = inputChain.MassPerM;

            NodeResolution = inputChain.NodeResolution;
            int minNodeSegments = 3;
            int maxNodeSegments = 10000;

            Nodes = new List<S_Node>(); //nodes that construct the chain
            newGlobalNodes = new List<S_Node>(); //new nodes (with/without ends) to add to the "global" list

            #endregion

            #region 02 - GENERATE NODES

            //converting existing nodes to points
            Point3dList allNodesPts = new Point3dList();
            foreach (S_Node nd in allNodes)
                allNodesPts.Add(nd.Position);

            //dividing the lineCurve
            int div = (int)(InitialLineCurve.Line.Length / NodeResolution);
            if (div < minNodeSegments) div = minNodeSegments;
            if (div > maxNodeSegments) div = maxNodeSegments;

            InitialLineCurve.DivideByCount(div, true, out Point3d[] nodePts);

            //calculating the mass per node
            double nodeMass = 0;
            if (MassPerM > 0)
                nodeMass = (InitialLineCurve.Line.Length * MassPerM) / (div - 1);

            //creating each node
            for (int i = 0; i < nodePts.Length; i++)
            {
                Point3d pt = nodePts[i];

                #region get anchor data for constructor

                bool isAnchor = false;
                bool moveX = true;
                bool moveY = true;
                bool moveZ = true;

                int indexAnchor = inputAnchorPts.ClosestIndex(pt);
                if (pt.DistanceTo(inputAnchorPts[indexAnchor]) < tolerance)
                {
                    isAnchor = true;
                    moveX = inputAnchors[indexAnchor].MoveX;
                    moveY = inputAnchors[indexAnchor].MoveY;
                    moveZ = inputAnchors[indexAnchor].MoveZ;
                }

                #endregion

                S_Node node = new S_Node(pt, isAnchor, moveX, moveY, moveZ);

                bool isEndIteration = (i == 0) || (i == nodePts.Length - 1);

                //test if there is existing node at chain endpoints, otherwise just construct new nodes
                if (isEndIteration)
                {
                    //adding only the half of the mass for the chain endpoints

                    if (allNodesPts.Count > 0)
                    {
                        int clIndex = allNodesPts.ClosestIndex(pt);

                        if (pt.DistanceTo(allNodesPts[clIndex]) < tolerance)
                        {
                            //A1 - There is an existing node at chain endpoint
                            S_Node nodeEnd = allNodes[clIndex];
                            nodeEnd.Mass += nodeMass * 0.5;

                            Nodes.Add(nodeEnd); // adding existing Node to Chain nodes
                                                // nothing to add to newGlobalNodes
                        }
                        else
                        {
                            //A2 - There are no existing nodes at chain endpoint
                            node.Mass += nodeMass * 0.5;

                            Nodes.Add(node);
                            newGlobalNodes.Add(node);
                        }
                    }
                    else
                    {
                        //B - There are no existing nodes, create a new node at chain endpoint
                        node.Mass += nodeMass * 0.5;

                        Nodes.Add(node);
                        newGlobalNodes.Add(node);
                    }
                }
                else
                {
                    //C - Creating a new node between the chain endpoints
                    node.Mass += nodeMass;

                    Nodes.Add(node);
                    newGlobalNodes.Add(node);
                }

            }

            #endregion

        }

        /// <summary>
        /// empty chain constructor
        /// </summary>
        public S_Chain()
        {
            Index = -1;
            ID = Guid.NewGuid();
            IsValid = false;
            SimulationPhase = -1;
            InitialLineCurve = null;
            Elasticity = double.NaN;
            MaxLength = double.NaN;
            PolyLength = double.NaN;
            MassPerM = double.NaN;
            NodeResolution = double.NaN;
            Nodes = new List<S_Node>();
        }

        #endregion

        #region METHODS

        public void UpdatePolyLength()
        {
            double distance = 0;

            for (int i = 0; i < Nodes.Count - 1; i++)
                distance += Nodes[i].Position.DistanceTo(Nodes[i + 1].Position);

            PolyLength = distance;
        }

        public void ResetChainMassToZero()
        {
            foreach (var node in Nodes)
                node.Mass = 0;
        }

        public void UpdateChainMass()
        {

            double nodeMass = (PolyLength * MassPerM) / (Nodes.Count - 1);

            for (int i = 0; i < Nodes.Count; i++)
            {
                if ((i == 0) || (i == Nodes.Count - 1)) //end iterations
                {
                    Nodes[i].Mass += nodeMass * 0.5;
                }
                else
                {
                    Nodes[i].Mass += nodeMass;
                }
            }

        }

        public static void ComputeChainsNodeNeighbours(List<S_Chain> allChains)
        {
            foreach (S_Chain chain in allChains)
            {
                for (int i = 0; i < chain.Nodes.Count; i++)
                {
                    if (i == 0)
                    {
                        //adding node
                        chain.Nodes[i].Neighbours.Add(chain.Nodes[i + 1]);
                        //adding chain
                        chain.Nodes[i].NeighbourChains.Add(chain);
                    }
                    else if (i == chain.Nodes.Count - 1)
                    {
                        //adding node
                        chain.Nodes[i].Neighbours.Add(chain.Nodes[i - 1]);
                        //adding chain
                        chain.Nodes[i].NeighbourChains.Add(chain);
                    }
                    else
                    {
                        //adding node
                        chain.Nodes[i].Neighbours.Add(chain.Nodes[i - 1]);
                        //adding chain
                        chain.Nodes[i].NeighbourChains.Add(chain);

                        //adding node
                        chain.Nodes[i].Neighbours.Add(chain.Nodes[i + 1]);
                        //adding chain
                        chain.Nodes[i].NeighbourChains.Add(chain);
                    }
                }
            }
        }

        public Curve GenerateInterpolatedCurve(int degree)
        {
            if (degree % 2 == 0)
                degree -= 1;
            if (degree < 1)
                degree = 1;
            if (degree > 11)
                degree = 11;

            Point3dList pts = new Point3dList();
            foreach (var node in Nodes)
                pts.Add(node.Position);

            Curve crv = Curve.CreateInterpolatedCurve(pts, degree);

            if (crv != null && crv.IsValid)
                return crv;
            else
                return null;
        }

        public Polyline GeneratePolyline()
        {
            Point3dList pts = new Point3dList();
            foreach (var node in Nodes)
                pts.Add(node.Position);

            Polyline poly = new Polyline(pts);

            if (poly != null && poly.IsValid)
                return poly;
            else
                return null;
        }

        public NurbsCurve GenerateNurbsCurve(int degree)
        {
            if (degree < 1)
                degree = 1;
            if (degree > 11)
                degree = 11;

            //NurbsCurve nrb = new NurbsCurve(degree, Nodes.Count);
            //for (int i = 0; i < Nodes.Count; i++)
            //nrb.Points.SetPoint(i, Nodes[i].Position);

            List<Point3d> pts = new List<Point3d>();
            foreach (var node in Nodes)
                pts.Add(node.Position);

            NurbsCurve nrb = NurbsCurve.Create(false, degree, pts);

            return nrb;
        }

        public Curve GenerateRebuildedCurve(int degree, int count, bool keepTangents)
        {
            if (degree < 1)
                degree = 1;
            if (degree > 11)
                degree = 11;

            if (count < 2)
                count = 2;
            if (count > 1000)
                count = 1000;

            Curve crv = GenerateInterpolatedCurve(degree).ToNurbsCurve();
            NurbsCurve nrb2 = crv.Rebuild(count, degree, keepTangents);
            Curve crv2 = nrb2.DuplicateCurve();

            return crv2;
        }

        public static void GetAllInterpolatedCurves(List<S_Chain> allChains, int degree, out List<Curve> curves)
        {
            curves = new List<Curve>();

            foreach (var ch in allChains)
            {
                Curve crv = ch.GenerateInterpolatedCurve(degree);
                curves.Add(crv);
            }
        }

        public static void GetAllPolylines(List<S_Chain> allChains, out List<Polyline> polylines)
        {
            polylines = new List<Polyline>();

            foreach (var ch in allChains)
            {
                Polyline poly = ch.GeneratePolyline();
                polylines.Add(poly);
            }
        }

        public static void GetAllNurbsCurves(List<S_Chain> allChains, int degree, out List<NurbsCurve> nurbsCurves)
        {
            nurbsCurves = new List<NurbsCurve>();

            foreach (var ch in allChains)
            {
                NurbsCurve nrbscrv = ch.GenerateNurbsCurve(degree);
                nurbsCurves.Add(nrbscrv);
            }
        }

        public static void GetAllRebuildedCurves(List<S_Chain> allChains, int degree, int count, bool keepTangents,
                                                 out List<Curve> curves)
        {
            curves = new List<Curve>();

            foreach (var ch in allChains)
            {
                Curve crv = ch.GenerateRebuildedCurve(degree, count, keepTangents);
                curves.Add(crv);
            }
        }


        public static bool GetWeightDiagrams(List<S_Chain> allChains, bool displayWeight, bool displayLoad,
            Color colourWeight, Color colourLoad, bool useGradient, double scale,
            out Mesh diagramWeight, out Mesh diagramLoad)
        {
            #region testing input data

            diagramWeight = null;
            diagramLoad = null;

            if (allChains == null || allChains.Count < 1)
                return false;

            if (!displayWeight && !displayLoad)
                return false;

            if (colourWeight.IsEmpty || colourLoad.IsEmpty)
                return false;

            if (double.IsNaN(scale))
                return false;

            if (scale < 0) scale = Math.Abs(scale);
            if (scale < 1e-6) scale = 1e-6;

            #endregion

            diagramWeight = new Mesh();
            diagramLoad = new Mesh();

            #region get intervals

            double maxW = 0;
            double maxL = 0;

            foreach (S_Chain ch in allChains)
                foreach (S_Node nd in ch.Nodes)
                {
                    if ((nd.Mass) > maxW)
                        maxW = nd.Mass;

                    if ((nd.Load) > maxL)
                        maxL = nd.Load;
                }

            Interval sourceW = new Interval(0, maxW);
            Interval sourceL = new Interval(0, maxL);
            Interval target = new Interval(0, scale);

            #endregion

            foreach (S_Chain ch in allChains)
            {
                ch.WeightDiagram(displayWeight, displayLoad, colourWeight, colourLoad,
                    useGradient, sourceW, sourceL, target, out Mesh dWeight, out Mesh dLoad);

                if (dWeight != null && dWeight.IsValid && dWeight.Vertices.Count > 0)
                    diagramWeight.Append(dWeight);

                if (dLoad != null && dLoad.IsValid && dLoad.Vertices.Count > 0)
                    diagramLoad.Append(dLoad);
            }

            if (displayWeight == false)
                diagramWeight = null;

            if (displayLoad == false)
                diagramLoad = null;

            return true;
        }


        public void WeightDiagram(bool displayWeight, bool displayLoad,
                                Color colourWeight, Color colourLoad,
                                bool useGradient, Interval sourceW, Interval sourceL, Interval target,
                                out Mesh dWeight, out Mesh dLoad)
        {
            dWeight = new Mesh();
            dLoad = new Mesh();

            Interval sourceBoth = new Interval(0, sourceW.T1 + sourceL.T1);

            #region test

            double maxMass = 0;
            double maxLoad = 0;

            foreach (S_Node nd in Nodes)
            {
                if (nd.Mass > maxMass) maxMass = nd.Mass;
                if (nd.Load > maxLoad) maxLoad = nd.Load;
            }

            if (maxMass < 1e-6) displayWeight = false;
            if (maxLoad < 1e-6) displayLoad = false;

            if (displayWeight == false && displayLoad == false)
                return;

            #endregion;

            #region get direction

            Vector3d vecSum = new Vector3d(0, 0, 0);

            foreach (S_Node nd in Nodes)
                vecSum += new Vector3d(nd.Position - nd.InitialPosition);

            Vector3d dir = new Vector3d(0, 0, 1);
            if (vecSum.Z < 0)
                dir = new Vector3d(0, 0, -1);

            #endregion


            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                S_Node n1 = Nodes[i];
                S_Node n2 = Nodes[i + 1];

                if (displayWeight && displayLoad)
                {
                    if (n1.Mass == 0 && n2.Mass == 0 && n1.Load == 0 && n2.Load == 0)
                        continue;

                    Mesh tempWeight = new Mesh();
                    Mesh tempLoad = new Mesh();

                    Point3d pt1 = n1.Position;
                    double d1w = S_Solver.RemapNumber(n1.Mass, sourceBoth, target);
                    double d1l = S_Solver.RemapNumber(n1.Mass + n1.Load, sourceBoth, target);

                    Point3d pt2 = n2.Position;
                    double d2w = S_Solver.RemapNumber(n2.Mass, sourceBoth, target);
                    double d2l = S_Solver.RemapNumber(n2.Mass + n2.Load, sourceBoth, target);

                    tempWeight.Vertices.AddVertices(new Point3dList(4) { pt1, pt1 + dir * d1w, pt2 + dir * d2w, pt2 });
                    tempWeight.Faces.AddFace(0, 1, 2, 3);

                    tempLoad.Vertices.AddVertices(new Point3dList(4) { pt1 + dir * d1w, pt1 + dir * d1l, pt2 + dir * d2l, pt2 + dir * d2w });
                    tempLoad.Faces.AddFace(0, 1, 2, 3);

                    Color cw1 = colourWeight;
                    Color cw2 = colourWeight;

                    Color cl1 = colourLoad;
                    Color cl2 = colourLoad;

                    if (useGradient)
                    {
                        int kw1 = (int)S_Solver.RemapNumber(n1.Mass, sourceW, new Interval(0, 255));
                        int kw2 = (int)S_Solver.RemapNumber(n2.Mass, sourceW, new Interval(0, 255));

                        cw1 = Color.FromArgb(kw1, cw1);
                        cw2 = Color.FromArgb(kw2, cw2);

                        int kl1 = (int)S_Solver.RemapNumber(n1.Load, sourceL, new Interval(0, 255));
                        int kl2 = (int)S_Solver.RemapNumber(n2.Load, sourceL, new Interval(0, 255));

                        cl1 = Color.FromArgb(kl1, cl1);
                        cl2 = Color.FromArgb(kl2, cl2);
                    }

                    tempWeight.VertexColors.SetColors(new Color[4] { cw1, cw1, cw2, cw2 });
                    tempLoad.VertexColors.SetColors(new Color[4] { cl1, cl1, cl2, cl2 });

                    dWeight.Append(tempWeight);
                    dLoad.Append(tempLoad);
                }
                else if (displayWeight)
                {
                    if (n1.Mass == 0 && n2.Mass == 0)
                        continue;

                    Mesh tempWeight = new Mesh();

                    Point3d pt1 = n1.Position;
                    double d1w = S_Solver.RemapNumber(n1.Mass, sourceW, target);
                    Point3d pt2 = n2.Position;
                    double d2w = S_Solver.RemapNumber(n2.Mass, sourceW, target);

                    tempWeight.Vertices.AddVertices(new Point3dList(4) { pt1, pt1 + dir * d1w, pt2 + dir * d2w, pt2 });
                    tempWeight.Faces.AddFace(0, 1, 2, 3);

                    Color c1 = colourWeight;
                    Color c2 = colourWeight;

                    if (useGradient)
                    {
                        int k1 = (int)S_Solver.RemapNumber(n1.Mass, sourceW, new Interval(0, 255));
                        int k2 = (int)S_Solver.RemapNumber(n2.Mass, sourceW, new Interval(0, 255));

                        c1 = Color.FromArgb(k1, c1);
                        c2 = Color.FromArgb(k2, c2);
                    }

                    tempWeight.VertexColors.SetColors(new Color[4] { c1, c1, c2, c2 });
                    dWeight.Append(tempWeight);
                }
                else if (displayLoad)
                {
                    if (n1.Load == 0 && n2.Load == 0)
                        continue;

                    Mesh tempLoad = new Mesh();

                    Point3d pt1 = n1.Position;
                    double d1w = S_Solver.RemapNumber(n1.Load, sourceL, target);
                    Point3d pt2 = n2.Position;
                    double d2w = S_Solver.RemapNumber(n2.Load, sourceL, target);

                    tempLoad.Vertices.AddVertices(new Point3dList(4) { pt1, pt1 + dir * d1w, pt2 + dir * d2w, pt2 });
                    tempLoad.Faces.AddFace(0, 1, 2, 3);

                    Color c1 = colourLoad;
                    Color c2 = colourLoad;

                    if (useGradient)
                    {
                        int k1 = (int)S_Solver.RemapNumber(n1.Load, sourceL, new Interval(0, 255));
                        int k2 = (int)S_Solver.RemapNumber(n2.Load, sourceL, new Interval(0, 255));

                        c1 = Color.FromArgb(k1, c1);
                        c2 = Color.FromArgb(k2, c2);
                    }

                    tempLoad.VertexColors.SetColors(new Color[4] { c1, c1, c2, c2 });

                    dLoad.Append(tempLoad);
                }
            }

            if (displayWeight)
            {
                int c = 0;
                dWeight.Faces.RemoveZeroAreaFaces(ref c);
                dWeight.Vertices.CullUnused();
                dWeight.Compact();

                //dWeight.Vertices.CombineIdentical(true, true);
                dWeight.Weld(RhinoMath.ToRadians(10));
                dWeight.Normals.ComputeNormals();
                dWeight.UnifyNormals();
                dWeight.Compact();
            }

            if (displayLoad)
            {
                int c = 0;
                dLoad.Faces.RemoveZeroAreaFaces(ref c);
                dLoad.Vertices.CullUnused();
                dLoad.Compact();

                //dLoad.Vertices.CombineIdentical(true, true);
                dLoad.Weld(RhinoMath.ToRadians(10));
                dLoad.Normals.ComputeNormals();
                dLoad.UnifyNormals();
                dLoad.Compact();
            }
        }

        public static bool GetDisplacementDiagram(List<S_Chain> allChains, Color colour, bool useGradient, bool useFalseColours, out Mesh diagram)
        {
            #region testing input data

            diagram = null;

            if (allChains == null || allChains.Count < 1)
                return false;

            if (colour.IsEmpty)
                return false;

            #endregion

            diagram = new Mesh();

            #region get intervals

            double maxD = 0;

            foreach (S_Chain ch in allChains)
                foreach (S_Node nd in ch.Nodes)
                {
                    double d = nd.Position.DistanceTo(nd.InitialPosition);

                    if (d > maxD)
                        maxD = d;
                }

            Interval source = new Interval(0, maxD);

            #endregion

            foreach (S_Chain ch in allChains)
            {
                ch.DisplacementDiagram(source, colour, useGradient, useFalseColours, out Mesh dChain);

                if (dChain != null && dChain.IsValid && dChain.Vertices.Count > 0) //&& dWeight.IsValid
                    diagram.Append(dChain);
            }

            return true;
        }

        public void DisplacementDiagram(Interval source, Color colour, bool useGradient, bool useFalseColours, out Mesh dChain)
        {
            dChain = new Mesh();
            double k = 0.05;
            int alpha = Convert.ToInt32(255 * k);

            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                S_Node n1 = Nodes[i];
                S_Node n2 = Nodes[i + 1];

                Mesh tempCh = new Mesh();

                Point3d p1s = n1.InitialPosition;
                Point3d p1e = n1.Position;
                Point3d p2s = n2.InitialPosition;
                Point3d p2e = n2.Position;

                tempCh.Vertices.AddVertices(new Point3dList(4) { p1s, p1e, p2e, p2s });
                tempCh.Faces.AddFace(0, 1, 2, 3);

                Color c1s = Color.FromArgb(alpha, colour);
                Color c1e = colour;
                Color c2s = Color.FromArgb(alpha, colour);
                Color c2e = colour;

                if (useFalseColours)
                {
                    Vector3d vec1 = new Vector3d(n1.Position - n1.InitialPosition);
                    vec1.Unitize();
                    vec1 += new Vector3d(1, 1, 1);
                    vec1 *= 0.5;
                    vec1 *= 255;
                    c1e = Color.FromArgb(255, (int)vec1.X, (int)vec1.Y, (int)vec1.Z);

                    Vector3d vec2 = new Vector3d(n2.Position - n2.InitialPosition);
                    vec2.Unitize();
                    vec2 += new Vector3d(1, 1, 1);
                    vec2 *= 0.5;
                    vec2 *= 255;
                    c2e = Color.FromArgb(255, (int)vec2.X, (int)vec2.Y, (int)vec2.Z);
                }

                if (useGradient)
                {
                    int k1 = (int)S_Solver.RemapNumber(n1.Position.DistanceTo(n1.InitialPosition), source, new Interval(0, 255));
                    int k2 = (int)S_Solver.RemapNumber(n2.Position.DistanceTo(n2.InitialPosition), source, new Interval(0, 255));

                    c1e = Color.FromArgb(k1, c1e);
                    c1s = Color.FromArgb(Convert.ToInt32(k1 * k), c1e);
                    c2e = Color.FromArgb(k2, c2e);
                    c2s = Color.FromArgb(Convert.ToInt32(k2 * k), c2e);
                }

                tempCh.VertexColors.SetColors(new Color[4] { c1s, c1e, c2e, c2s });
                dChain.Append(tempCh);

            }

            int c = 0;
            dChain.Faces.RemoveZeroAreaFaces(ref c);
            dChain.Vertices.CullUnused();
            dChain.Compact();

            //dWeight.Vertices.CombineIdentical(true, true);
            dChain.Weld(RhinoMath.ToRadians(10));
            dChain.Normals.ComputeNormals();
            dChain.UnifyNormals();
            dChain.Compact();

        }



        #endregion

    }
}
