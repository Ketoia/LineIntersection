using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CrossingLines : MonoBehaviour
{
    public Material lineMat;
    public Material sphereMat;
    public Mesh simplePlaneMesh;
    public GUISkin guiStyle;

    [SerializeField] private List<Line> lineList = new List<Line>();
    [SerializeField] private List<EventPoint> eventPointList = new List<EventPoint>();

    private List<Vector2> intersectionList = new List<Vector2>();

    [SerializeField] private RenderTexture linesTexture;
    [SerializeField] private RenderTexture spheresTexture;

    public RawImage linesImage;
    public RawImage spheresImage;
    private float sizeOfIntersection = 0.04f;

    private bool renderLines = false;
    private bool renderSpheres = false;

    private bool enableShowingIntersaction = true;
    private int lineCount = 5;
    private float lastTime = 0;
    private System.Diagnostics.Stopwatch timer;

    //UI
    private bool manageMultipleLines = false;
    private bool manageTwoLines = false;

    //Dwie linie
    private Line firstLine;
    private Line secondLine;
    private float clampOffset = 5; //Blokowanie wartoœci punktów ¿eby unikn¹æ wyjœcia za ekran
    private string xIntersection = "";
    private string yIntersection = "";


    void Start()
    {
        timer = new System.Diagnostics.Stopwatch();

        spheresTexture = new RenderTexture(Screen.width, Screen.height, 0);
        linesTexture = new RenderTexture(Screen.width, Screen.height, 0);

        linesImage.texture = linesTexture;
        spheresImage.texture = spheresTexture;

        linesImage.color = new Color(1, 1, 1, 1);
        spheresImage.color = new Color(1, 1, 1, 1);

        Camera.onPostRender += ShowLines;
        Camera.onPostRender += ShowSpheres;
    }

    void OnDestroy()
    {
        Camera.onPostRender -= ShowLines;
        Camera.onPostRender -= ShowSpheres;
    }

    void OnGUI()
    {
        if (!(manageTwoLines || manageMultipleLines))
        {
            GUILayout.BeginVertical(guiStyle.FindStyle("customBox"));
            if (GUILayout.Button("Dwie linie"))
            {
                //clear
                lineList.Clear();
                intersectionList.Clear();

                renderLines = true;
                renderSpheres = true;

                sizeOfIntersection = 0.04f;

                //add init values
                firstLine = new Line(
                    new Vector2(Random.Range(-clampOffset, clampOffset), Random.Range(-clampOffset, clampOffset)),
                    new Vector2(Random.Range(-clampOffset, clampOffset), Random.Range(-clampOffset, clampOffset)));

                secondLine = new Line(
                    new Vector2(Random.Range(-clampOffset, clampOffset), Random.Range(-clampOffset, clampOffset)),
                    new Vector2(Random.Range(-clampOffset, clampOffset), Random.Range(-clampOffset, clampOffset)));
                lineList.Add(firstLine);
                lineList.Add(secondLine);

                manageTwoLines = true;
            }
            if (GUILayout.Button("Wiele losowych lini"))
            {
                //clear values
                lineList.Clear();
                intersectionList.Clear();

                renderLines = true;
                renderSpheres = true;

                manageMultipleLines = true;
            }
            GUILayout.EndVertical();
        }

        if (manageMultipleLines)
            ManageMultipleLinesUI();
        if (manageTwoLines)
            ManageTwoLinesUI();

    }

    private void ManageMultipleLinesUI()
    {
        GUILayout.BeginVertical(guiStyle.FindStyle("customBox"));

        GUILayout.BeginHorizontal();
        GUILayout.Box("Liczba losowych lini: " + lineCount, guiStyle.box);
        lineCount = Mathf.FloorToInt( GUILayout.HorizontalSlider(lineCount, 2, 5000, guiStyle.horizontalSlider, guiStyle.horizontalSliderThumb));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Box("Wielkoœæ punktów przeciêæ: " + sizeOfIntersection, guiStyle.box);
        sizeOfIntersection = RoundTo3(GUILayout.HorizontalSlider(sizeOfIntersection, 0.001f, 0.1f, guiStyle.horizontalSlider, guiStyle.horizontalSliderThumb));
        GUILayout.EndHorizontal();

        enableShowingIntersaction = GUILayout.Toggle(enableShowingIntersaction, "Wyœwietlaj przeciêcia", guiStyle.toggle);
        spheresImage.gameObject.SetActive(enableShowingIntersaction);

        if (GUILayout.Button("Wylosuj linie", guiStyle.button))
        {
            lineList.Clear();
            intersectionList.Clear();
            float offset = 5;
            for (int i = 0; i < lineCount; i++)
            {
                Line line = new Line(
                    new Vector2(Random.Range(-offset, offset), Random.Range(-offset, offset)),
                    new Vector2(Random.Range(-offset, offset), Random.Range(-offset, offset)));

                lineList.Add(line);
            }

            renderLines = true;
        }

        if (GUILayout.Button("Naiwne wykrywanie przeciêæ", guiStyle.button))
        {
            timer.Restart();
            NaiveIntersection();
            lastTime = timer.ElapsedMilliseconds * 0.001f;
            timer.Stop();
        }

        if (GUILayout.Button("Wykrywanie przeciêæ z u¿yciem algorytmu bentley'a", guiStyle.button))
        {
            timer.Restart();
            BentleyAlghoritm();
            lastTime = timer.ElapsedMilliseconds * 0.001f;
            timer.Stop();
        }

        GUILayout.Box("Ostatni czas wykrywania w sekundach: " + lastTime.ToString(), guiStyle.box);
        if (GUILayout.Button("Cofnij", guiStyle.button))
        {
            lineList.Clear();
            intersectionList.Clear();
            renderLines = true;
            renderSpheres = true;

            manageMultipleLines = false;
        }
        GUILayout.EndVertical();
    }

    private void ManageTwoLinesUI()
    {
        intersectionList.Clear();

        GUILayout.BeginVertical(guiStyle.FindStyle("customBox"));

        GUILayout.Label("WprowadŸ koordynaty pierwszej lini, gdzie ich wartoœci s¹ -5 do 5");
        GUILayout.BeginHorizontal();
        firstLine.begin.x = ShowSlider(firstLine.begin.x, "x_1");
        firstLine.begin.y = ShowSlider(firstLine.begin.y, "y_1");
        firstLine.end.x = ShowSlider(firstLine.end.x, "x_2");
        firstLine.end.y = ShowSlider(firstLine.end.y, "y_2");
        GUILayout.EndHorizontal();

        GUILayout.Label("WprowadŸ koordynaty drugiej lini, gdzie ich wartoœci s¹ -5 do 5");
        GUILayout.BeginHorizontal();
        secondLine.begin.x = ShowSlider(secondLine.begin.x, "x_1");
        secondLine.begin.y = ShowSlider(secondLine.begin.y, "y_1");
        secondLine.end.x = ShowSlider(secondLine.end.x, "x_2");
        secondLine.end.y = ShowSlider(secondLine.end.y, "y_2");
        GUILayout.EndHorizontal();


        //Check intersection
        if (LineSegementsIntersect(firstLine, secondLine, out Vector2 intersectionPoint))
        {
            intersectionList.Add(intersectionPoint);

            xIntersection = intersectionPoint.x.ToString("F3");
            yIntersection = intersectionPoint.y.ToString("F3");

            GUILayout.Label("Przecinaj¹ siê w punkcie: x = " + xIntersection + ", y = " + yIntersection);
        }
        else
        {
            GUILayout.Label("Odcinki nie przecinaj¹ siê");
        }


        //Enable rendering
        renderLines = true;
        renderSpheres = true;

        if (GUILayout.Button("Cofnij"))
        {
            lineList.Clear();
            intersectionList.Clear();
            manageTwoLines = false;
        }
        GUILayout.EndVertical();
    }

    private float ShowSlider(float inValue, string name)
    {
        GUILayout.BeginVertical();
        GUILayout.Label(name, guiStyle.label);
        float outValue = RoundTo3(GUILayout.HorizontalSlider(inValue, -5, 5, guiStyle.horizontalSlider, guiStyle.horizontalSliderThumb));
        GUILayout.Label(outValue.ToString(), guiStyle.label);
        GUILayout.EndVertical();
        return outValue;
    }

    private float RoundTo3(float inValue)
    {
        float outValue;
        outValue = (float)decimal.Round((decimal)inValue, 3);
        return outValue;
    }

    private void ShowLines(Camera cam)
    {
        if (!renderLines)
            return;

        if (!lineMat)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }

        var textureCache = RenderTexture.active;
        RenderTexture.active = linesTexture;

        GL.PushMatrix();
        lineMat.SetPass(0);
        GL.Clear(true, true, Color.clear);
        GL.Begin(GL.LINES);
        foreach (var line in lineList)
        {
            GL.Vertex(line.begin);
            GL.Vertex(line.end);
        }
        GL.End();
        GL.PopMatrix();

        RenderTexture.active = textureCache;

        ClearTexture(spheresTexture);
        renderLines = false;
    }

    private void ShowSpheres(Camera cam)
    {
        if (!renderSpheres)
            return;

        if (!sphereMat)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }
        float offset = sizeOfIntersection;

        var textureCache = RenderTexture.active;
        RenderTexture.active = spheresTexture;

        GL.PushMatrix();
        sphereMat.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.Clear(true, true, Color.clear);

        foreach (var spherePos in intersectionList)
        {
            GL.Vertex3(spherePos.x - offset, spherePos.y - offset, 0);
            GL.Vertex3(spherePos.x - offset, spherePos.y + offset, 0);
            GL.Vertex3(spherePos.x + offset, spherePos.y + offset, 0);
            GL.Vertex3(spherePos.x + offset, spherePos.y - offset, 0);
        }

        GL.End();
        GL.PopMatrix();

        RenderTexture.active = textureCache;

        renderSpheres = false;
    }

    private void ClearTexture(RenderTexture rt)
    {
        var textureCache = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.Clear(true, true, Color.clear);

        GL.End();
        GL.PopMatrix();

        RenderTexture.active = textureCache;

    }

    private void NaiveIntersection()
    {
        intersectionList.Clear();
        int size = lineList.Count;

        for (int i = 0; i < size; i++)
        {
            Line line1 = lineList[i];
            for (int j = i + 1; j < size; j++)
            {
                Line line2 = lineList[j];

                if (LineSegementsIntersect(line1, line2, out Vector2 intersectionPoint))
                {
                    intersectionList.Add(intersectionPoint);
                }
            }
        }

        //foreach (var line1 in lineList)
        //{
        //    foreach (var line2 in lineList)
        //    {
        //        if (line1 != line2)
        //        {
        //            if (LineSegementsIntersect(line1, line2, out Vector2 intersectionPoint))
        //            {
        //                intersectionList.Add(intersectionPoint);
        //            }
        //        }
        //    }
        //}

        renderSpheres = true;
    }

    private void BentleyAlghoritm()
    {
        intersectionList.Clear();
        eventPointList = SortLinesByY();

        List<Line> status = new List<Line>();

        int count = eventPointList.Count;

        for (int i = 0; i < count; i++)
        {
            EventPoint eventPoint = eventPointList[i];

            if (eventPoint.isBegin)
            {
                //Check intersection with every line in status
                foreach (var line in status)
                {
                    if (LineSegementsIntersect(eventPoint.line, line, out Vector2 intersectionPoint))
                    {
                        intersectionList.Add(intersectionPoint);
                    }
                }

                //Add to status
                status.Add(eventPoint.line);
            }
            else
            {
                status.Remove(eventPoint.line);
            }
        }

        renderSpheres = true;
    }

    private List<EventPoint> SortLinesByY()
    {
        List<EventPoint> eventPoints = new List<EventPoint>();

        foreach (var line in lineList)
        {
            eventPoints.Add(new EventPoint { line = line, pos = line.begin, isBegin = true });
            eventPoints.Add(new EventPoint { line = line, pos = line.end, isBegin = false });
        }

        eventPoints.Sort(delegate (EventPoint x, EventPoint y)
        {
            if (x.pos.y == y.pos.y) return 0;
            else if (x.pos.y >= y.pos.y) return -1;
            else return 1;
        });

        return eventPoints;
    }

    private bool LineSegementsIntersect(Line line1, Line line2, out Vector2 intersectionPoint)
    {
        Vector2 r = line1.end - line1.begin;
        Vector2 s = line2.end - line2.begin;

        float d = r.x * s.y - r.y * s.x;
        float u = ((line2.begin.x - line1.begin.x) * r.y - (line2.begin.y - line1.begin.y) * r.x) / d;
        float t = ((line2.begin.x - line1.begin.x) * s.y - (line2.begin.y - line1.begin.y) * s.x) / d;

        intersectionPoint = u * s + line2.begin;

        return (0 <= u && u <= 1 && 0 <= t && t <= 1);
    }

    [System.Serializable]
    private class EventPoint
    {
        public bool isBegin = false;
        public Vector2 pos;
        public Line line;
    }

    [System.Serializable]
    private class Line
    {
        public Vector2 begin;
        public Vector2 end; //event point

        public Line(Vector2 begin, Vector2 end)
        {
            //Pocz¹tek jest zawsze na górze, a koniec na dole

            if (begin.y > end.y)
            {
                this.begin = begin;
                this.end = end;
            }
            else
            {
                this.begin = end;
                this.end = begin;
            }
        }
    }
}
