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
using SpiderGH.components;

namespace SpiderGH
{
    public class CombineData : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CombineData class.
        /// </summary>
        public CombineData()
          : base("Combine Data", "Combine Data",
              "Prepare and test the data for the simulation. Use Chains' (CH') and Nodes' (ND').",
              Universal.Category(), Universal.SubCategory_Processor())
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chain", "CH", "Chain data", GH_ParamAccess.list);
            pManager.AddGenericParameter("Anchor", "AN", "Anchor data", GH_ParamAccess.list);
            pManager.AddGenericParameter("Load", "LD", "Load data", GH_ParamAccess.list);

            pManager[0].DataMapping = GH_DataMapping.Flatten;
            pManager[1].DataMapping = GH_DataMapping.Flatten;
            pManager[2].DataMapping = GH_DataMapping.Flatten;

            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Chain Preview", "P-CH", "Preview of the initial CHAIN positions", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchor Preview", "P-AN", "Preview of the initial ANCHOR positions", GH_ParamAccess.list);
            pManager.AddPointParameter("Node Preview", "P-ND", "Preview of the initial NODE positions", GH_ParamAccess.list);
            pManager.AddNumberParameter("Node Mass", "M-ND", "Mass of the each node", GH_ParamAccess.list);
            pManager.AddNumberParameter("Node Load", "L-ND", "Load of the each node", GH_ParamAccess.list);

            pManager.AddGenericParameter("Chains'", "CH'", "Chain data for simulation", GH_ParamAccess.list);
            pManager.AddGenericParameter("Nodes'", "ND'", "Node data for simulation", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            #region INPUT DATA 

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<S_Chain> inputChains = new List<S_Chain>();
            DA.GetDataList(0, inputChains);

            List<S_Anchor> inputAnchors = new List<S_Anchor>();
            DA.GetDataList(1, inputAnchors);

            List<S_Load> inputLoads = new List<S_Load>();
            DA.GetDataList(2, inputLoads);

            #endregion

            #region TEST DATA

            if (inputChains.Count > 0)
            {
                for (int i = 0; i < inputChains.Count; i++)
                {
                    if (!inputChains[i].IsValid)
                    {
                        inputChains.RemoveAt(i);
                        i -= 1;
                    }

                    if (inputChains.Count < 1)
                        break;
                }
            }

            if (inputAnchors.Count > 0)
            {
                for (int i = 0; i < inputAnchors.Count; i++)
                {
                    if (!inputAnchors[i].IsValid)
                    {
                        inputAnchors.RemoveAt(i);
                        i -= 1;
                    }

                    if (inputAnchors.Count < 1)
                        break;
                }
            }

            if (inputLoads.Count > 0)
            {
                for (int i = 0; i < inputLoads.Count; i++)
                {
                    if (!inputLoads[i].IsValid)
                    {
                        inputLoads.RemoveAt(i);
                        i -= 1;
                    }

                    if (inputLoads.Count < 1)
                        break;
                }
            }

            if (inputChains.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough input chains");
                return;
            }

            if (inputAnchors.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough input anchors");
                return;
            }

            #endregion

            #region ADDITIONAL CHECK FOR ANCHORS

            //get curves
            List<Curve> crvs = new List<Curve>();
            foreach (var chain in inputChains)
                crvs.Add(new LineCurve(chain.InitialLineCurve.Line).DuplicateCurve());

            //update the positions of the anchors
            foreach (var anchor in inputAnchors)
            {
                bool test = S_Solver.PointOnClosestCurves(anchor.Position, crvs, out Point3d pt, out int ix, out double d);

                if (test && d < anchor.DistanceTolerance && d > 1e-6)
                    anchor.Position = pt;
            }

            //delete anchors that are not close to curves
            for (int i = 0; i < inputAnchors.Count; i++)
            {
                bool onCurve = S_Solver.IsPointOnCurves(inputAnchors[i].Position, crvs, inputAnchors[i].DistanceTolerance);

                if (!onCurve)
                {
                    inputAnchors.RemoveAt(i);
                    i -= 1;
                }

                if (inputAnchors.Count < 1)
                    break;
            }

            if (inputAnchors.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough anchors near input curves");
                return;
            }

            //remove duplicate anchors

            for (int i = 1; i < inputAnchors.Count; i++)
            {
                Point3d pt = inputAnchors[i].Position;

                Point3dList ptsTemp = new Point3dList();

                for (int k = 0; k < i; k++)
                    ptsTemp.Add(inputAnchors[k].Position);

                double distance = pt.DistanceTo(ptsTemp[ptsTemp.ClosestIndex(pt)]);

                if (distance < tolerance * 10)
                {
                    inputAnchors.RemoveAt(i);
                    i -= 1;
                }

                if (inputAnchors.Count < 1)
                    break;
            }

            #region old 
            /*
            Point3dList pts1 = new Point3dList();
            foreach (var anchor in inputAnchors)
                pts1.Add(anchor.Position);

            S_Solver.RemoveDuplicatePoints(pts1, tolerance*10, out Point3dList pts2);

            for (int i = 0; i < inputAnchors.Count; i++)
            {
                Point3d pt = inputAnchors[i].Position;
                double distance = pt.DistanceTo(pts2[pts2.ClosestIndex(pt)]);

                if (distance > tolerance*10)
                {
                    inputAnchors.RemoveAt(i);
                    i -= 1;
                }

                if (inputAnchors.Count < 1)
                    break;
            }
            */
            #endregion

            if (inputAnchors.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some duplicates were deleted & Not enough anchors near input curves");
                return;
            }

            int fixedAnchors = 0;
            foreach (var anchor in inputAnchors)
            {
                if (!anchor.MoveX && !anchor.MoveY && !anchor.MoveZ)
                    fixedAnchors++;
            }

            if (fixedAnchors < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "At least one Anchor needs to be fixed - Setting Anchor[0] translations to False");
                inputAnchors[0].MoveX = false;
                inputAnchors[0].MoveY = false;
                inputAnchors[0].MoveZ = false;
            }

            #endregion

            #region ADDITIONAL CHECK FOR LOADS

            List<S_Load> allLoads = new List<S_Load>();
            Point3dList ptLoads = new Point3dList();

            if (inputLoads.Count > 0)
            {
                //filter point loads and update their positions
                foreach (var load in inputLoads)
                    if (load.Type == S_Load.LoadType.PointLoad && load.IsValid)
                    {
                        bool test = S_Solver.PointOnClosestCurves(load.Position, crvs, out Point3d closestPt, out int ix, out double d);

                        if (test && d < load.DistanceTolerance)
                        {
                            load.Position = closestPt;
                            ptLoads.Add(load.Position);
                            allLoads.Add(load);
                        }
                    }

                //filter linear loads
                foreach (var load in inputLoads)
                    if (load.Type == S_Load.LoadType.LineLoad && load.IsValid)
                        allLoads.Add(load);

                //filter region loads
                foreach (var load in inputLoads)
                    if (load.Type == S_Load.LoadType.BrepLoad && load.IsValid)
                        allLoads.Add(load);
            }

            #endregion

            #region ADDITIONAL CHECK FOR CHAINS

            //get points to split curves

            Point3dList ptsSplit1 = new Point3dList();
            foreach (var anchor in inputAnchors)
                ptsSplit1.Add(anchor.Position);

            ptsSplit1.AddRange(ptLoads);

            foreach (var chain in inputChains)
            {
                ptsSplit1.Add(chain.InitialLineCurve.Line.From);
                ptsSplit1.Add(chain.InitialLineCurve.Line.To);
            }

            S_Solver.RemoveDuplicatePoints(ptsSplit1, tolerance * 10, out Point3dList ptsSplit2);

            //splitting chains
            List<S_Chain> inputChainsSet2 = new List<S_Chain>();
            foreach (var chain in inputChains)
            {
                bool testSplit = S_Solver.SplitLineCurve(chain.InitialLineCurve, ptsSplit2, tolerance, tolerance * 10,
                                                            out List<LineCurve> segments);

                if (!testSplit || segments.Count == 1)
                {
                    S_Chain nChain = new S_Chain(chain.InitialLineCurve.Line, chain.Elasticity, chain.MassPerM, chain.NodeResolution);
                    inputChainsSet2.Add(nChain);
                }
                else
                {
                    foreach (var lncrv in segments)
                    {
                        S_Chain nChain = new S_Chain(lncrv.Line, chain.Elasticity, chain.MassPerM, chain.NodeResolution);
                        inputChainsSet2.Add(nChain);
                    }
                }
            }



            #endregion

            #region GENERATE CHAINS AND NODES

            //inputChainsSet2, inputAnchors

            List<S_Node> allNodes = new List<S_Node>();
            List<S_Chain> allChains = new List<S_Chain>();

            Point3dList inputAnchorPts = new Point3dList();
            foreach (var anchor in inputAnchors)
                inputAnchorPts.Add(anchor.Position);

            for (int i = 0; i < inputChainsSet2.Count; i++)
            {
                S_Chain nChain = new S_Chain(i, inputChainsSet2[i], inputAnchors, inputAnchorPts,
                    allNodes, tolerance, out List<S_Node> newNodes);

                allChains.Add(nChain);
                allNodes.AddRange(newNodes);
            }

            S_Node.UpdateNodeIndices(allNodes);

            S_Chain.ComputeChainsNodeNeighbours(allChains);

            foreach (var chain in allChains)
                chain.SimulationPhase = 1;
            foreach (var node in allNodes)
                node.SimulationPhase = 1;

            S_Node.AddPointLoads(allNodes, allLoads);
            S_Node.AddLinearLoads(allNodes, allLoads);
            S_Node.AddRegionLoads(allNodes, allLoads);

            #endregion

            #region OUTPUT DATA

            List<GH_Line> ghLines = new List<GH_Line>();
            foreach (var chain in allChains)
                ghLines.Add(new GH_Line(chain.InitialLineCurve.Line));

            List<GH_Point> ghAnchors = new List<GH_Point>();
            List<GH_Point> ghNodes = new List<GH_Point>();
            List<GH_Number> ghNodeMass = new List<GH_Number>();
            List<GH_Number> ghNodeLoads = new List<GH_Number>();

            foreach (var node in allNodes)
            {
                if (node.IsAnchor)
                {
                    ghAnchors.Add(new GH_Point(node.InitialPosition));
                }
                else
                {
                    ghNodes.Add(new GH_Point(node.InitialPosition));
                    ghNodeMass.Add(new GH_Number(node.Mass));
                    ghNodeLoads.Add(new GH_Number(node.Load));
                }
            }

            DA.SetDataList(0, ghLines);
            DA.SetDataList(1, ghAnchors);
            DA.SetDataList(2, ghNodes);
            DA.SetDataList(3, ghNodeMass);
            DA.SetDataList(4, ghNodeLoads);

            DA.SetDataList(5, allChains);
            DA.SetDataList(6, allNodes);

            #endregion
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return SpiderGH.Properties.Resources._02_CombineData;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("cb7ea387-6770-4e3a-88a6-afc9b7f27302"); }
        }
    }
}