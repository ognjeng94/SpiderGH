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
using SpiderGH.Properties;
using SpiderGH.components;

namespace SpiderGH
{
    public class DisplayWeights : GH_Component
    {

        Color red = Color.FromArgb(204, 82, 41);
        Color blue = Color.FromArgb(41, 150, 204);

        /// <summary>
        /// Initializes a new instance of the DisplayWeights class.
        /// </summary>
        public DisplayWeights()
          : base("Display Weights", "Display NW",
              "Display Node Weights",
              Universal.Category(), Universal.SubCategory_PostProcessor())
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
            pManager.AddBooleanParameter("Weight", "dW", "Display node weights", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Loads", "dL", "Display node loads", GH_ParamAccess.item, true);
            pManager.AddColourParameter("Colour W", "cW", "Colour for the 1st diagram", GH_ParamAccess.item, red);
            pManager.AddColourParameter("Colour L", "cL", "Colour for the 2nd diagram", GH_ParamAccess.item, blue);
            pManager.AddBooleanParameter("Gradient", "G", "Display with gradient", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Scale", "S", "Scale", GH_ParamAccess.item, 1.00);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "poly", "Polylines", GH_ParamAccess.list);
            pManager.AddMeshParameter("Weight D", "DW", "Weight diagram", GH_ParamAccess.item);
            pManager.AddMeshParameter("Load D", "DL", "Load diagram", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region input data

            List<S_Chain> allChains = new List<S_Chain>();
            DA.GetDataList(0, allChains);

            bool displayWeight = true;
            DA.GetData(1, ref displayWeight);

            bool displayLoad = true;
            DA.GetData(2, ref displayLoad);

            Color colourWeight = red;
            DA.GetData(3, ref colourWeight);

            Color colourLoad = blue;
            DA.GetData(4, ref colourLoad);

            bool useGradient = true;
            DA.GetData(5, ref useGradient);

            double scale = 1.0;
            DA.GetData(6, ref scale);

            #endregion

            #region testing data

            if (allChains.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough chains or nodes");
                return;
            }

            if (double.IsNaN(scale)) scale = 1;
            if (scale < 0) scale = Math.Abs(scale);
            if (scale < 1e-6) scale = 1e-6;

            if (colourWeight.IsEmpty) colourWeight = red;
            if (colourLoad.IsEmpty) colourLoad = blue;

            #endregion

            #region solver

            //get polylines
            S_Chain.GetAllPolylines(allChains, out List<Polyline> polylines);

            //get meshes
            bool test = S_Chain.GetWeightDiagrams(allChains, displayWeight, displayLoad,
                                                colourWeight, colourLoad, useGradient, scale,
                                                out Mesh diagramWeight, out Mesh diagramLoad);

            if (!test)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid computation");
                return;
            }

            #endregion

            #region output

            DA.SetDataList(0, polylines);
            DA.SetData(1, diagramWeight);
            DA.SetData(2, diagramLoad);

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
                return Resources._03_DisplayWeights;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("745B0221-3F35-4FD0-AD39-927DC8FAC6E0"); }
        }
    }
}