using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WinForms;

namespace SelectVertices
{
    class SelectVerticesEditInteractiveOverlay : EditInteractiveOverlay
    {
        private PointStyle controlPointStyle;
        private PointStyle selectedControlPointStyle;

        public SelectVerticesEditInteractiveOverlay()
            : base()
        {
            //We add the column "IsSelected" to ExistingControlPointsLayer that will be used to keep track if the control point has been dragged.
            //Notice that the column "State" already exists. It is used for keeping tracked if the control point is being dragged.
            this.ExistingControlPointsLayer.Open();
            this.ExistingControlPointsLayer.Columns.Add(new FeatureSourceColumn("IsSelected"));
            this.ExistingControlPointsLayer.Close();
        }

        //Style for drawing a control point not selected
        public PointStyle ControlPointStyle
        {
            get { return controlPointStyle; }
            set { controlPointStyle = value; }
        }

        //Style for drawing a control point when selected (being dragged or after having being dragged)
        public PointStyle SelectedControlPointStyle
        {
            get { return selectedControlPointStyle; }
            set { selectedControlPointStyle = value; }
        }

        //Sets the column value for "IsSelected" to "true" if control point selected.
        protected override void OnControlPointSelected(ControlPointSelectedEditInteractiveOverlayEventArgs e)
        {
            base.OnControlPointSelected(e);

            foreach (Feature feature in this.ExistingControlPointsLayer.InternalFeatures)
            {
                if (feature.ColumnValues["State"] == "selected")
                {
                    feature.ColumnValues["IsSelected"] = "true";
                }
            }
        }

        //At the Mouse Up event, the features of ExistingControlPointsLayer will be cleared and new one will 
        //be generated for better performance. So it is necessary to reupdate the column values of "IsSelected" to the new 
        //ExistingControlPointsLayer.
        protected override InteractiveResult MouseUpCore(InteractionArguments interactionArguments)
        {
            Collection<int> draggedPointsIndex = new Collection<int>();

            //Before the actual MouseUp event, we get the indexes of the features with value "true" of column "IsSelected".
            this.ExistingControlPointsLayer.Open();
            Collection<Feature> draggedPoints = this.ExistingControlPointsLayer.QueryTools.GetAllFeatures(new string[] { "IsSelected" });
            this.ExistingControlPointsLayer.Close();

            for (int index = 0; index < draggedPoints.Count; index++)
            {
                if (draggedPoints[index].ColumnValues["IsSelected"] == "true")
                {
                    draggedPointsIndex.Add(index);
                }
            }

            //The MouseUp is called clearing ExistingControlPointsLayer. (We do that for better performance)
            InteractiveResult interactiveResult = base.MouseUpCore(interactionArguments);

            //Now, we need to reset the column values of "IsSelected" for the relevant features.
            foreach (int index in draggedPointsIndex)
            {
                this.ExistingControlPointsLayer.InternalFeatures[index].ColumnValues["IsSelected"] = "true";
            }
            this.ExistingControlPointsLayer.Close();

            return interactiveResult;
        }

        //Overrides the DrawCore function.
        protected override void DrawCore(GeoCanvas canvas)
        {
            //Draws the Edit Shapes as default.
            Collection<SimpleCandidate> labelsInAllLayers = new Collection<SimpleCandidate>();
            EditShapesLayer.Open();
            EditShapesLayer.Draw(canvas, labelsInAllLayers);
            canvas.Flush();

            //Gets the control points and draw its features according to the value of "IsSelected" column.
            this.ExistingControlPointsLayer.Open();
            Collection<Feature> ExistingControlPoints = this.ExistingControlPointsLayer.QueryTools.GetAllFeatures(new string[1] { "IsSelected" });
            ExistingControlPointsLayer.Close();

            //Loops thru the control points features and check for the value of "IsSelected" collumn.
            foreach (Feature feature in ExistingControlPoints)
            {
                if (feature.ColumnValues.ContainsKey("IsSelected") && feature.ColumnValues["IsSelected"] == "true")
                {
                    Feature[] features = new Feature[1] { feature };
                    selectedControlPointStyle.Draw(features, canvas, labelsInAllLayers, labelsInAllLayers);
                }
                else
                {
                    Feature[] features = new Feature[1] { feature };
                    controlPointStyle.Draw(features, canvas, labelsInAllLayers, labelsInAllLayers);
                }
            }
        }
    }
}
