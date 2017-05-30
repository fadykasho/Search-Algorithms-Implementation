﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Search.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Search
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Binding Propery

        public ObservableCollection<string> SearchTypes { get; set; } =
            new ObservableCollection<string> { "DEPTH FIRST", "British MUSEUM", "DEPTH FIRST WITH FILTER" };
        public ObservableCollection<string> StartLocations { get; set; }
            = new ObservableCollection<string>();
        public ObservableCollection<string> GoolLocations { get; set; }
            = new ObservableCollection<string>();
        public ObservableCollection<string> Maps { get; set; } =
            new ObservableCollection<string>() { "SMALL", "MEDIUM", "LARGE" };
        public ObservableCollection<string> SearchSpeed { get; set; } =
            new ObservableCollection<string>() { "X1", "X2", "X4" };
        public string SelectedSearchType { get; set; }
        public string StartLocation { get; set; }
        public string GoolLocation { get; set; }
        public string SelectedMap { get; set; }
        public string SelectedSearchSpeed { get; set; }
        
        #endregion

        #region feilds

        int speed, treePathCount, treePathChildCount;
        float canvasWidth, canvasHeight, startWidth, startHeight, width, height, circleRadius;
        float[] xAxis, yAxis;
        List<Node> nodes;
        Node sL, gL, startLocationOfPreviousNode, goolLocationOfPreviousNode;
        List<Road> AvaiableRoads { get; set; } = new List<Road>();
        List<List<Node>> MySearchPath { get; set; } = new List<List<Node>>();
        List<Node> Walks { get; set; } = new List<Node>();
        DispatcherTimer SearchSpeedTick = new DispatcherTimer();
        bool drawPath, drawCorrentRoad;

        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            SelectedSearchType = SearchTypes[0];
            SelectedMap = Maps[0];
            SelectedSearchSpeed = SearchSpeed[0];
            burningText = "Cool!";
            CreateFlameEffect();
            SearchSpeedTick.Tick += SearchSpeedTick_Tick;
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, 500);
        }

        

        private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            // burning Cool! Text 
            var ds = args.DrawingSession;
            SetupText(sender);
            ConfigureEffect(args.Timing);
            ds.DrawImage(composite, width, height - startHeight * 2);
        }

        private void canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Draw the Map Path
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].ConnectedNode.Count; j++)
                {
                    args.DrawingSession.DrawLine(nodes[i].X, nodes[i].Y, nodes[i].ConnectedNode[j].X, nodes[i].ConnectedNode[j].Y, Colors.Gray, circleRadius / 2);
                }
            }

            // Draw the Searching Path
            if (Walks.Count > 1)
            {
                for (int i = 0; i < Walks.Count - 1; i++)
                {
                    args.DrawingSession.DrawLine(Walks[i].X, Walks[i].Y, Walks[i + 1].X, Walks[i + 1].Y, Colors.Red, circleRadius / 3);
                }
            }

            // Draw the Corrent Path
            if (AvaiableRoads.Count > 0 && drawCorrentRoad)
            {
                for (int i = AvaiableRoads[0].PassedRoad.Count - 1; i > 0; i--)
                {
                    args.DrawingSession.DrawLine(AvaiableRoads[0].PassedRoad[i].X, AvaiableRoads[0].PassedRoad[i].Y,
                        AvaiableRoads[0].PassedRoad[i - 1].X, AvaiableRoads[0].PassedRoad[i - 1].Y, Colors.LightGreen, circleRadius / 4);
                }
            }

            // Draw the Node
            for (int i = 0; i < nodes.Count; i++)
            {
                args.DrawingSession.FillCircle(nodes[i].X, nodes[i].Y, circleRadius, Colors.Black);

                var textformat = new CanvasTextFormat() { FontSize = circleRadius, WordWrapping = CanvasWordWrapping.NoWrap, FontFamily = "Arial" };
                textformat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                textformat.VerticalAlignment = CanvasVerticalAlignment.Center;
                args.DrawingSession.DrawText(nodes[i].NodeName, nodes[i].X, nodes[i].Y, Colors.White, textformat);

                if (nodes[i].StartPoint)
                {
                    args.DrawingSession.DrawCircle(nodes[i].X, nodes[i].Y, circleRadius, Colors.Red, circleRadius / 4);
                }
                if (nodes[i].GoolPoint)
                {
                    args.DrawingSession.DrawCircle(nodes[i].X, nodes[i].Y, circleRadius, Colors.LightGreen, circleRadius / 4);
                }
            }
            
        }


        private void container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvasWidth = (float)container.ActualWidth;
            startWidth = canvasWidth / 10;
            width = canvasWidth - startWidth;
            canvasHeight = (float)container.ActualHeight;
            startHeight = canvasHeight / 20;
            height = canvasHeight - startHeight;
            xAxis = new float[10] { startWidth, width / 5, 3 * width / 10, 2 * width / 5, width / 2, 3 * width / 5, 7 * width / 10, 4 * width / 5, 9 * width / 10, width };
            yAxis = new float[5] { startHeight, height / 4, height / 2, 3 * height / 4, height };
            //fontSize = circleRadius;
            if (canvasHeight > canvasWidth)
            {
                circleRadius = canvasWidth / 25;
            }
            else
            {
                
                circleRadius = canvasHeight / 25;
            }
            
            
            if (SelectedMap == Maps[0])
            {
                initializeMap1();
            }
            else if (SelectedMap == Maps[1])
            {
                initializeMap2();
            }
        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            #region
            
            if (sL != null && gL != null)
            {
                StopAnimation();
                Road road = new Road() { HeadNode = sL };
                foreach (var item in road.HeadNode.ConnectedNode)
                {
                    road.PossibleRoad.Enqueue(item);
                }
                if (SelectedSearchType == SearchTypes[0])
                {
                    DepthFirstSearch(road);
                    DrawPath();
                    SearchSpeedTick.Start();
                    
                }
                else if (SelectedSearchType == SearchTypes[1])
                    BrithishMuseum(road);
                
            }
            
            #endregion
        }

        private void DepthFirstSearch(Road road)
        {
            // if there isn't connected node roll back we rich the end of the tell

            if (road.HeadNode.GoolPoint)
            {
                road.PassedRoad.Insert(0, road.HeadNode);
                MySearchPath.Add(road.PassedRoad);
                AvaiableRoads.Add(road);
            }
            else if (road.PossibleRoad.Count <= 0)
            {
                road.PassedRoad.Insert(0, road.HeadNode);
                MySearchPath.Add(road.PassedRoad);
                return;
            }
            // if there is connected node
            else
            {
                // check if we already find one path if we do roll back
                if (AvaiableRoads.Count <= 0)
                {
                    while (road.PossibleRoad.Count != 0 && AvaiableRoads.Count <= 0)
                    {
                        // if not extend the node
                        Road extendedRoad = new Road();
                        extendedRoad.PassedRoad.Add(road.HeadNode);
                        foreach (var item in road.PassedRoad)
                            extendedRoad.PassedRoad.Add(item);

                        extendedRoad.HeadNode = road.PossibleRoad.Dequeue();

                        // add the possible road for the extended node
                        foreach (var item in extendedRoad.HeadNode.ConnectedNode)
                        {
                            bool passed = false;
                            foreach (var pastroad in extendedRoad.PassedRoad)
                            {
                                if (pastroad.NodeName == item.NodeName)
                                    passed = true;
                            }
                            if (!passed)
                                extendedRoad.PossibleRoad.Enqueue(item);
                        }
                        DepthFirstSearch(extendedRoad);
                    }

                }
            }
        }

        private void BrithishMuseum(Road road)
        {

        }

        private void ComboBox_SearchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation if it's drawings
            StopAnimation();

            canvascontroll.Invalidate();
        }

        private void ComboBox_StartLocationsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation
            StopAnimation();

            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(value))
            {
                // initialze the previouseNode
                if (startLocationOfPreviousNode != null)
                {
                    startLocationOfPreviousNode.StartPoint = false;
                }
                startLocationOfPreviousNode = nodes.Find(n => n.NodeName == value);
                // add logo to start location
                sL = nodes.Find(n => n.NodeName == value);
                sL.StartPoint = true;
                // update the ui
                canvascontroll.Invalidate();
            }

        }

        private void ComboBox_GoolLocationSelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation
            StopAnimation();
            
            // Get The New Select Gool Location
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(value))
            {
                // remove the Gool logo from the old Gool location
                if (goolLocationOfPreviousNode != null)
                {
                    goolLocationOfPreviousNode.GoolPoint = false;
                }

                goolLocationOfPreviousNode = nodes.Find(n => n.NodeName == value);
                gL = nodes.Find(n => n.NodeName == value);
                gL.GoolPoint = true;
                
                canvascontroll.Invalidate();
            }
        }

        private void ComboBox_MapsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopAnimation();
            if (canvasWidth > 0 && SelectedMap != Maps[2])
            {
                AvaiableRoads.Clear();
                nodes = null;
                startLocationOfPreviousNode = null;
                goolLocationOfPreviousNode = null;
                sL = null;
                gL = null;
                if (SelectedMap == Maps[0])
                {
                    initializeMap1();
                }
                else if (SelectedMap == Maps[1])
                {
                    initializeMap2();
                }
                canvascontroll.Invalidate();
            }
        }

        private void ComboBox_SpeedSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            if (value == SearchSpeed[0])
            {
                speed = 500;
            }
            else if (value == SearchSpeed[1])
            {
                speed = 250;
            }
            else
            {
                speed = 125;
            }
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, speed);
        }

        // update points after change the map
        private void UpdatePoints()
        {
            StartLocations.Clear();
            GoolLocations.Clear();
            foreach (var item in nodes)
            {
                StartLocations.Add(item.NodeName);
                GoolLocations.Add(item.NodeName);
            }
        }
        
        private void SearchSpeedTick_Tick(object sender, object e)
        {
            DrawPath();
        }

        private void DrawPath()
        {
            if (treePathChildCount == -1)
            {
                Walks.Clear();
                drawPath = true;
                treePathCount += 1;
                if (treePathCount == MySearchPath.Count)
                {
                    drawCorrentRoad = true;
                    SearchSpeedTick.Stop();
                    treePathCount = 0;
                    canvascontroll.Invalidate();
                    return;
                }
            }
            if (treePathCount < MySearchPath.Count && drawPath == true)
            {
                treePathChildCount = MySearchPath[treePathCount].Count - 1;
                drawPath = false;
            }
            if (treePathCount != MySearchPath.Count && treePathChildCount != -1)
            {
                Walks.Add(MySearchPath[treePathCount][treePathChildCount]);
                treePathChildCount -= 1;
                canvascontroll.Invalidate();
            }
        }

        private void StopAnimation()
        {
            treePathCount = 0;
            treePathChildCount = 0;
            drawPath = true;
            SearchSpeedTick.Stop();
            drawCorrentRoad = false;
            Walks.Clear();
            AvaiableRoads.Clear();
            MySearchPath.Clear();
            canvascontroll.Invalidate();
        }
        private void initializeMap1()
        {
            if (nodes == null)
            {
                Node S = new Node() { NodeName = "S" };
                Node A = new Node() { NodeName = "A" };
                Node B = new Node() { NodeName = "B" };
                Node C = new Node() { NodeName = "C" };
                Node D = new Node() { NodeName = "D" };
                Node E = new Node() { NodeName = "E" };
                Node G = new Node() { NodeName = "G" };


                S.ConnectedNode = new List<Node>() { A, B };
                A.ConnectedNode = new List<Node>() { S, C, B };
                B.ConnectedNode = new List<Node>() { S, A, D };
                C.ConnectedNode = new List<Node>() { A, E, D };
                D.ConnectedNode = new List<Node>() { B, C, G };
                E.ConnectedNode = new List<Node>() { C };
                G.ConnectedNode = new List<Node>() { D };

                nodes = new List<Node>() { S, A, B, C, D, E, G };
                UpdatePoints();
            }
            // S
            nodes[0].X = xAxis[1];
            nodes[0].Y = yAxis[3];
            // A
            nodes[1].X = xAxis[3];
            nodes[1].Y = yAxis[2];
            // B
            nodes[2].X = xAxis[4];
            nodes[2].Y = yAxis[3];
            // C
            nodes[3].X = xAxis[5];
            nodes[3].Y = yAxis[1];
            // D
            nodes[4].X = xAxis[6];
            nodes[4].Y = yAxis[3];
            // E
            nodes[5].X = xAxis[7];
            nodes[5].Y = yAxis[1];
            // G
            nodes[6].X = xAxis[7];
            nodes[6].Y = yAxis[2];


        }

        private void initializeMap2()
        {
            if (nodes == null)
            {
                #region 
                Node A = new Node() { NodeName = "A" };
                Node B = new Node() { NodeName = "B" };
                Node C = new Node() { NodeName = "C" };
                Node D = new Node() { NodeName = "D" };
                Node E = new Node() { NodeName = "E" };
                Node F = new Node() { NodeName = "F" };
                Node G = new Node() { NodeName = "G" };
                Node H = new Node() { NodeName = "H" };
                Node I = new Node() { NodeName = "I" };
                Node J = new Node() { NodeName = "J" };
                Node K = new Node() { NodeName = "K" };
                Node L = new Node() { NodeName = "L" };
                Node M = new Node() { NodeName = "M" };
                Node N = new Node() { NodeName = "N" };


                A.ConnectedNode = new List<Node>() { B, C };
                B.ConnectedNode = new List<Node>() { A, D, F };
                C.ConnectedNode = new List<Node>() { A, E, H };
                D.ConnectedNode = new List<Node>() { B, I, G };
                E.ConnectedNode = new List<Node>() { F, C, K };
                F.ConnectedNode = new List<Node>() { B, E, G };
                G.ConnectedNode = new List<Node>() { D, F, J };
                H.ConnectedNode = new List<Node>() { C, K, N };
                I.ConnectedNode = new List<Node>() { D, L };
                J.ConnectedNode = new List<Node>() { G };
                K.ConnectedNode = new List<Node>() { E, H };
                L.ConnectedNode = new List<Node>() { I, M };
                M.ConnectedNode = new List<Node>() { L, N };
                N.ConnectedNode = new List<Node>() { H, M };
                nodes = new List<Node>() { A, B, C, D, E, F, G, H, I, J, K, L, M, N };
                UpdatePoints();
                #endregion
            }
            // A
            nodes[0].X = xAxis[0];
            nodes[0].Y = yAxis[2];
            // B
            nodes[1].X = xAxis[1];
            nodes[1].Y = yAxis[1];
            // C
            nodes[2].X = xAxis[1];
            nodes[2].Y = yAxis[3];
            // D
            nodes[3].X = xAxis[3];
            nodes[3].Y = yAxis[0];
            // E
            nodes[4].X = xAxis[2];
            nodes[4].Y = yAxis[2];
            // F
            nodes[5].X = xAxis[2];
            nodes[5].Y = yAxis[1];
            // G
            nodes[6].X = xAxis[4];
            nodes[6].Y = yAxis[1];
            // H
            nodes[7].X = xAxis[3];
            nodes[7].Y = yAxis[4];
            // I
            nodes[8].X = xAxis[6];
            nodes[8].Y = yAxis[0];
            // J
            nodes[9].X = xAxis[5];
            nodes[9].Y = yAxis[2];
            // K
            nodes[10].X = xAxis[4];
            nodes[10].Y = yAxis[3];
            // L
            nodes[11].X = xAxis[7];
            nodes[11].Y = yAxis[1];
            // M
            nodes[12].X = xAxis[8];
            nodes[12].Y = yAxis[2];
            // N
            nodes[13].X = xAxis[6];
            nodes[13].Y = yAxis[3];
        }

        #region Burning Text
        CanvasCommandList textCommandList;
        MorphologyEffect morphology;
        CompositeEffect composite;
        Transform2DEffect flameAnimation;
        Transform2DEffect flamePosition;

        string burningText;
        float fontSize = 30.0f;


        /// <summary>
        /// Calculates a good font size so the text will fit even on smaller phone screens.
        /// </summary>
        private static float GetFontSize(Size displaySize)
        {
            const float maxFontSize = 72;
            const float scaleFactor = 24;
            return Math.Min((float)displaySize.Width / scaleFactor, maxFontSize);
        }
        /// <summary>
        /// Renders text into a command list and sets this as the input to the flame
        /// effect graph. The effect graph must already be created before calling this method.
        /// </summary>
        private void SetupText(ICanvasResourceCreator resourceCreator)
        {
            textCommandList = new CanvasCommandList(resourceCreator);

            using (var ds = textCommandList.CreateDrawingSession())
            {
                ds.Clear(Color.FromArgb(0, 0, 0, 0));

                ds.DrawText(
                    burningText,
                    0,
                    0,
                    Colors.SteelBlue,
                    new CanvasTextFormat
                    {
                        FontFamily = "Segoe UI",
                        FontSize = fontSize,
                        HorizontalAlignment = CanvasHorizontalAlignment.Center,
                        VerticalAlignment = CanvasVerticalAlignment.Top
                    });
            }

            // Hook up the command list to the inputs of the flame effect graph.
            morphology.Source = textCommandList;
            composite.Sources[1] = textCommandList;
        }
        private void ConfigureEffect(CanvasTimingInformation timing)
        {
            // Animate the flame by shifting the Perlin noise upwards (-Y) over time.
            flameAnimation.TransformMatrix = Matrix3x2.CreateTranslation(0, -(float)timing.TotalTime.TotalSeconds * 60.0f);

            // Scale the flame effect 2x vertically, aligned so it starts above the text.
            float verticalOffset = fontSize * 1.4f;

            var centerPoint = new Vector2(0, verticalOffset);

            flamePosition.TransformMatrix = Matrix3x2.CreateScale(1, 2, centerPoint);
        }

        // Alternative entrypoint for use by AppIconGenerator.
        private void CreateFlameEffect()
        {
            // Thicken the text.
            morphology = new MorphologyEffect
            {
                // The Source property is set by SetupText().
                Mode = MorphologyEffectMode.Dilate,
                Width = 7,
                Height = 1
            };

            // Blur, then colorize the text from black to red to orange as the alpha increases.
            var colorize = new ColorMatrixEffect
            {
                Source = new GaussianBlurEffect
                {
                    Source = morphology,
                    BlurAmount = 3f
                },
                ColorMatrix = new Matrix5x4
                {
                    M11 = 0f,
                    M12 = 0f,
                    M13 = 0f,
                    M14 = 0f,
                    M21 = 0f,
                    M22 = 0f,
                    M23 = 0f,
                    M24 = 0f,
                    M31 = 0f,
                    M32 = 0f,
                    M33 = 0f,
                    M34 = 0f,
                    M41 = 0f,
                    M42 = 1f,
                    M43 = 0f,
                    M44 = 1f,
                    M51 = 1f,
                    M52 = -0.5f,
                    M53 = 0f,
                    M54 = 0f
                }
            };

            // Generate a Perlin noise field (see flamePosition).
            // Animate the noise by modifying flameAnimation's transform matrix at render time.
            flameAnimation = new Transform2DEffect
            {
                Source = new BorderEffect
                {
                    Source = new TurbulenceEffect
                    {
                        Frequency = new Vector2(0.109f, 0.109f),
                        Size = new Vector2(500.0f, 80.0f)
                    },
                    // Use Mirror extend mode to allow us to spatially translate the noise
                    // without any visible seams.
                    ExtendX = CanvasEdgeBehavior.Mirror,
                    ExtendY = CanvasEdgeBehavior.Mirror
                }
            };

            // Give the flame its wavy appearance by generating a displacement map from the noise
            // (see flameAnimation) and applying this to the text.
            // Stretch and position this flame behind the original text.
            flamePosition = new Transform2DEffect
            {
                Source = new DisplacementMapEffect
                {
                    Source = colorize,
                    Displacement = flameAnimation,
                    Amount = 40.0f
                }
                // Set the transform matrix at render time as it depends on window size.
            };

            // Composite the text over the flames.
            composite = new CompositeEffect()
            {
                Sources = { flamePosition, null }
            };
        }
        #endregion

        
    }
}