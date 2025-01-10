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
    public class ChainSimulation : GH_Component
    {


        #region PROPERTIES

        int Iteration;
        bool SuccessfulSimulation;
        double IterationDisplacement;

        int GlobalMaxIterations = 250000;

        List<S_Node> allNodes;
        List<S_Chain> allChains;

        #endregion

        /// <summary>
        /// Initializes a new instance of the ChainSimulation class.
        /// </summary>
        public ChainSimulation()
          : base("Chain Simulation", "Simulation",
              "Chain simulation with preview of the each iteration.",
              Universal.Category(), Universal.SubCategory_Processor())
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
            pManager.AddGenericParameter("Chains'", "CH'", "Chain data for simulation", GH_ParamAccess.list);
            pManager.AddGenericParameter("Nodes'", "ND'", "Node data for simulation", GH_ParamAccess.list);

            pManager.AddBooleanParameter("Parallel computing", "Parallel", "If true, parallel computing is activated", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Trigger", "Trigger", "If you set this to 'True', you can use the 'trigger' component. " +
                                                                    "Otherwise component runs as fast as it can.", GH_ParamAccess.item, false);

            pManager.AddBooleanParameter("Update Mass", "Update Mass", "Update the mass for each chain at every iteration", GH_ParamAccess.item, false);
            pManager.AddVectorParameter("Gravity", "Gravity", "Gravity direction", GH_ParamAccess.item, Vector3d.ZAxis * 0.01);
            pManager.AddNumberParameter("Friction", "Friction", "Friction [0-1]. The smaller the number the slower the simulation", GH_ParamAccess.item, 0.9);

            pManager.AddIntegerParameter("Preview Nth", "Nth Step", "Preview every Nth iteration", GH_ParamAccess.item, 10);
            pManager.AddIntegerParameter("Substeps", "Substeps", "Number of additional substeps within each iteration. " +
                                                                "More steps = more precision & more time", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Max Velocity", "Max Vec", "Maximum velocity allowed per iteration", GH_ParamAccess.item, 0.5);

            pManager.AddIntegerParameter("Maximum number of iterations", "Iterations", "Maximum number of iterations for component to run (max: 250.000)",
                                                                                        GH_ParamAccess.item, 1000);
            pManager.AddNumberParameter("Maximum movement for Termination", "Termination", "Movement threshold for 'Solver Termination'",
                                                                                        GH_ParamAccess.item, 1e-6);

            pManager[0].DataMapping = GH_DataMapping.Flatten;
            pManager[1].DataMapping = GH_DataMapping.Flatten;

            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;
            pManager[12].Optional = true;
            pManager[13].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Chains'", "CH'", "Chain data ", GH_ParamAccess.list);
            pManager.AddGenericParameter("Nodes'", "ND'", "Node data", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Current iteration", "Iteration", "Current iteration", GH_ParamAccess.item);

            pManager.AddTextParameter("Text Data", "Data", "Additional simulation data", GH_ParamAccess.list);

            pManager.AddCurveParameter("Chain Preview", "P-CH", "Preview of the CHAINS", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchor Preview", "P-AN", "Preview of the ANCHOS", GH_ParamAccess.list);
            pManager.AddPointParameter("Node Preview", "P-ND", "Preview of the NODES", GH_ParamAccess.list);
            pManager.AddNumberParameter("Node Mass", "M-ND", "Mass of the each node", GH_ParamAccess.list);
            pManager.AddNumberParameter("Node Load", "L-ND", "Load of the each node", GH_ParamAccess.list);
            pManager.AddVectorParameter("Node Force", "F-ND", "Force vector of the each node", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region GET INPUT DATA

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            allNodes = new List<S_Node>();
            allChains = new List<S_Chain>();
            DA.GetDataList(0, allChains);
            DA.GetDataList(1, allNodes);

            bool parallel = false;
            DA.GetData(2, ref parallel);

            bool run = false;
            DA.GetData(3, ref run);

            bool reset = false;
            DA.GetData(4, ref reset);

            bool trigger = false;
            DA.GetData(5, ref trigger);

            bool updateMass = true;
            DA.GetData(6, ref updateMass);

            Vector3d gravity = Vector3d.ZAxis * 0.01;
            DA.GetData(7, ref gravity);

            double friction = 0.9;
            DA.GetData(8, ref friction);

            int previewNth = 10;
            DA.GetData(9, ref previewNth);

            int substeps = 10;
            DA.GetData(10, ref substeps);

            double maxAmplitude = 0.5;
            DA.GetData(11, ref maxAmplitude);

            int userMaxIteration = 500;
            DA.GetData(12, ref userMaxIteration);

            double termination = 1e-6;
            DA.GetData(13, ref termination);

            #endregion

            #region TEST DATA

            if (allChains.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough input chains");
                return;
            }

            if (allNodes.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough input nodes");
                return;
            }

            int stopSimulation = 0;
            if (Iteration < 1)
                foreach (var chain in allChains)
                    if (chain.SimulationPhase != 1)
                        stopSimulation++;

            if (stopSimulation > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some chains are not prepared for simulation. Please use output from CombineData component.");
                return;
            }

            if (gravity == Vector3d.Unset || gravity == Vector3d.Zero)
                gravity = Vector3d.ZAxis;

            if (friction < 0) friction = Math.Abs(friction);
            if (friction < tolerance * 10) friction = tolerance * 10;
            if (friction > 1) friction = 1;

            if (previewNth < 0) previewNth = Math.Abs(previewNth);
            if (previewNth < 1) previewNth = 1;
            if (previewNth > 10000) previewNth = 10000;

            if (substeps < 0) substeps = Math.Abs(substeps);
            if (substeps < 1) substeps = 1;
            if (substeps > 100) substeps = 100;

            if (maxAmplitude < 0) maxAmplitude = Math.Abs(maxAmplitude);
            if (maxAmplitude < tolerance * 10) maxAmplitude = tolerance * 10;
            if (maxAmplitude > 1000) maxAmplitude = 1000;

            if (userMaxIteration < 0) userMaxIteration = Math.Abs(userMaxIteration);
            if (userMaxIteration < 1) userMaxIteration = 1;
            if (userMaxIteration > GlobalMaxIterations) userMaxIteration = GlobalMaxIterations;

            if (termination < 0) termination = Math.Abs(termination);
            if (termination < 1e-9) termination = 1e-9;
            if (termination > 1000) termination = 1000;

            #endregion

            #region SOLVER STATES

            //reset
            if (Iteration < 1 || reset)
            {
                Iteration = 0;
                SuccessfulSimulation = false;
                IterationDisplacement = 0;

                //reset positions
                for (int i = 0; i < allNodes.Count; i++)
                    allNodes[i].ResetPositionsAndForces();

                foreach (var chain in allChains)
                    chain.SimulationPhase = 1;
                foreach (var node in allNodes)
                    node.SimulationPhase = 1;
            }

            //simulation
            if (!run)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Component is not running");
            }
            else if (Iteration >= GlobalMaxIterations || Iteration >= userMaxIteration)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Component has reached maximum number of iterations");
            }
            else if (SuccessfulSimulation)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Converged");
            }
            else
            {
                for (int x = 0; x < previewNth; x++)
                {
                    Iteration++;
                    IterationDisplacement = 0;

                    for (int k = 0; k < substeps; k++)
                    {
                        S_Solver.OneIteration(allNodes, allChains, parallel,
                                              gravity, friction, substeps, maxAmplitude, updateMass);

                        S_Node.MaxDisplacement(allNodes, out double displacement);
                        IterationDisplacement += displacement;
                    }

                    if (IterationDisplacement < termination)
                        SuccessfulSimulation = true;

                    if (SuccessfulSimulation)
                        break;
                }

                if (!trigger)
                    ExpireSolution(true);
            }

            if (Iteration > 0 || SuccessfulSimulation)
            {
                foreach (var chain in allChains)
                    chain.SimulationPhase = 2;
                foreach (var node in allNodes)
                    node.SimulationPhase = 2;
            }

            #endregion

            #region OUTPUT DATA

            List<GH_Curve> ghCurves = new List<GH_Curve>();
            foreach (var chain in allChains)
                ghCurves.Add(new GH_Curve(chain.GeneratePolyline().ToNurbsCurve()));

            List<GH_Point> ghAnchors = new List<GH_Point>();
            List<GH_Point> ghNodes = new List<GH_Point>();
            List<GH_Number> ghNodeMass = new List<GH_Number>();
            List<GH_Number> ghNodeLoads = new List<GH_Number>();
            List<GH_Vector> ghNodeForce = new List<GH_Vector>();

            foreach (var node in allNodes)
            {
                if (node.IsAnchor)
                {
                    ghAnchors.Add(new GH_Point(node.Position));
                }
                else
                {
                    ghNodes.Add(new GH_Point(node.Position));
                    ghNodeMass.Add(new GH_Number(node.Mass));
                    ghNodeLoads.Add(new GH_Number(node.Load));
                    ghNodeForce.Add(new GH_Vector(node.Force));
                }
            }

            List<string> info = new List<string>();
            info.Add("Number_of_Chains: " + allChains.Count.ToString());
            info.Add("Number_of_AllNodes: " + allNodes.Count.ToString());
            info.Add("Number_of_Anchors: " + ghAnchors.Count.ToString());
            info.Add("Number_of_Nodes: " + ghNodes.Count.ToString());
            info.Add("- - - - - - - - - - - - - - - - -");
            info.Add("Successful_Simulation: " + SuccessfulSimulation.ToString());
            info.Add("Termination_Movement: " + termination.ToString());
            info.Add("Iteration_Movement: " + IterationDisplacement.ToString());


            DA.SetDataList(0, allChains);
            DA.SetDataList(1, allNodes);
            DA.SetData(2, new GH_Integer(Iteration));
            DA.SetDataList(3, info);
            DA.SetDataList(4, ghCurves);
            DA.SetDataList(5, ghAnchors);
            DA.SetDataList(6, ghNodes);
            DA.SetDataList(7, ghNodeMass);
            DA.SetDataList(8, ghNodeLoads);
            DA.SetDataList(9, ghNodeForce);

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
                return Properties.Resources._02_ChainSimulation;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("03B3D950-C617-4882-A216-6DAE7CC726E4"); }
        }
    }
}