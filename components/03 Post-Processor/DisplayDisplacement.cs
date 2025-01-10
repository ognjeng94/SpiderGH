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
using Grasshopper.Kernel.Types.Transforms;
using SpiderGH.Properties;
using SpiderGH.components;


namespace SpiderGH
{
    public class DisplayDisplacement : GH_Component
    {

        Color red = Color.FromArgb(204, 82, 41);
        Color blue = Color.FromArgb(41, 150, 204);


        /// <summary>
        /// Initializes a new instance of the DisplayDisplacement class.
        /// </summary>
        public DisplayDisplacement()
          : base("Display Displacement", "Display ND",
              "Display Node Displacement",
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
            pManager.AddColourParameter("Colour", "C", "Base colour for the diagram", GH_ParamAccess.item, blue);
            pManager.AddBooleanParameter("Gradient", "G", "Display with gradient", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("False Colours", "FC", "Display with false colours", GH_ParamAccess.item, true);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "poly", "Polylines", GH_ParamAccess.list);
            pManager.AddMeshParameter("Diagram", "DD", "Displacement diagram", GH_ParamAccess.item);

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

            Color baseColour = blue;
            DA.GetData(1, ref baseColour);

            bool useGradient = true;
            DA.GetData(2, ref useGradient);

            bool useFalseColours = true;
            DA.GetData(3, ref useFalseColours);

            #endregion

            #region testing data

            if (allChains.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough chains or nodes");
                return;
            }

            if (baseColour.IsEmpty) baseColour = blue;

            #endregion

            #region solver

            //get polylines
            S_Chain.GetAllPolylines(allChains, out List<Polyline> polylines);

            //get meshes
            bool test = S_Chain.GetDisplacementDiagram(allChains, baseColour, useGradient, useFalseColours,
                                                        out Mesh diagram);

            if (!test)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid computation");
                return;
            }

            #endregion

            #region output

            DA.SetDataList(0, polylines);
            DA.SetData(1, diagram);

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
                return Resources._03_DisplayDisplacement;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("14E98DDC-FFA9-4B75-9B4A-8002FF024FAA"); }
        }
    }
}