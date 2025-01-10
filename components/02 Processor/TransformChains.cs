using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SpiderGH.components;

namespace SpiderGH
{
    public class TransformChains : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TransformChains class.
        /// </summary>
        public TransformChains()
          : base("Transform Chains", "Transform CH",
              "Transform Chains - Move, Rotate, Scale, Mirror, Orient... ",
              Universal.Category(), Universal.SubCategory_Processor())
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.tertiary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chains'", "CH'", "Chain data ", GH_ParamAccess.list);
            pManager.AddGenericParameter("Nodes'", "ND'", "Node data", GH_ParamAccess.list);

            pManager.AddTransformParameter("Transform", "xForm", "Transfrom", GH_ParamAccess.item);

            pManager[0].DataMapping = GH_DataMapping.Flatten;
            pManager[1].DataMapping = GH_DataMapping.Flatten;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Chain Preview", "P-CH", "Preview of the CHAINS", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchor Preview", "P-AN", "Preview of the ANCHOS", GH_ParamAccess.list);
            pManager.AddPointParameter("Node Preview", "P-ND", "Preview of the NODES", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //INPUT DATA

            List<S_Chain> allChains = new List<S_Chain>();
            DA.GetDataList(0, allChains);

            List<S_Node> allNodes = new List<S_Node>();
            DA.GetDataList(1, allNodes);

            Transform xform = new Transform();
            DA.GetData(2, ref xform);

            //TEST DATA
            #region TEST DATA

            int counterChains = 0;
            for (int i = 0; i < allChains.Count; i++)
            {
                if (allChains[i].SimulationPhase != 2)
                {
                    counterChains++;
                    allChains.RemoveAt(i);

                    i -= 1;
                }

                if (allChains.Count < 1)
                    break;
            }

            if (counterChains > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some chains were not simulated and were therefore deleted from the list");

            int counterNodes = 0;
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i].SimulationPhase != 2)
                {
                    counterNodes++;
                    allNodes.RemoveAt(i);

                    i -= 1;
                }

                if (allNodes.Count < 1)
                    break;
            }

            if (counterNodes > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some nodes were not simulated and were therefore deleted from the list");

            if (allChains.Count < 1 || allNodes.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough chains or nodes");
                return;
            }

            if (xform == Transform.Unset)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not a valid transform");
                return;
            }

            #endregion

            //OUTPUT DATA

            List<GH_Curve> ghCurves = new List<GH_Curve>();
            foreach (var chain in allChains)
            {
                Curve crv = chain.GeneratePolyline().ToNurbsCurve();
                crv.Transform(xform);
                ghCurves.Add(new GH_Curve(crv));
            }
            List<GH_Point> ghAnchors = new List<GH_Point>();
            List<GH_Point> ghNodes = new List<GH_Point>();

            foreach (var node in allNodes)
            {
                Point3d pt = node.Position;
                pt.Transform(xform);

                if (node.IsAnchor)
                {
                    ghAnchors.Add(new GH_Point(pt));
                }
                else
                {
                    ghNodes.Add(new GH_Point(pt));
                }
            }

            DA.SetDataList(0, ghCurves);
            DA.SetDataList(1, ghAnchors);
            DA.SetDataList(2, ghNodes);


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
                return SpiderGH.Properties.Resources._02_TransformChains;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5D363736-9ABA-4748-A998-0F96C9539E7A"); }
        }
    }
}