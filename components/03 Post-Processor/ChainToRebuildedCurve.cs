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
    public class ChainToRebuildedCurve : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ChainToRebuildedCurve class.
        /// </summary>
        public ChainToRebuildedCurve()
          : base("Chain To Rebuilded Curve", "CH Rebuilded",
              "Chain To Rebuilded Curve",
              Universal.Category(), Universal.SubCategory_PostProcessor())
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chains'", "CH'", "Chain data ", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Degree", "D", "Curve degree", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Point Count", "N", "Point count", GH_ParamAccess.item, 12);
            pManager.AddBooleanParameter("Keep Tangents", "T", "Keep end tangents", GH_ParamAccess.item, true);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Rebuilded Curves", "r-crv", "Rebuilded curves", GH_ParamAccess.list);

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

            int degree = 3;
            DA.GetData(1, ref degree);

            int count = 12;
            DA.GetData(2, ref count);

            bool keepTangents = true;
            DA.GetData(3, ref keepTangents);

            //TEST DATA
            if (allChains.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough chains or nodes");
                return;
            }

            //SOLVER
            S_Chain.GetAllRebuildedCurves(allChains, degree, count, keepTangents, out List<Curve> curves);

            //OUTPUT
            DA.SetDataList(0, curves);

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
                return SpiderGH.Properties.Resources._03_ChainRebuilded;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0F719BFA-8066-48DF-AD33-0E6F39237CCB"); }
        }
    }
}