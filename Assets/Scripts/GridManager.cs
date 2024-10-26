using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [SerializeField] GameObject p_Red;
    [SerializeField] GameObject p_Blue;
    [SerializeField] GameObject p_Yellow;
    [SerializeField] GameObject p_Purple;
    [SerializeField] GameObject p_Green;

    [SerializeField] AudioSource plopSFX, cascadeSFX;

    [SerializeField] int level;

    public enum C { RED, BLUE, YELLOW, PURPLE, GREEN };
    public GameObject[] colors;

    [SerializeField] Session sesh;

    [SerializeField] 
    public int pointsPerMatch = 100;

    public const int GRID_SIZE = 8;
    public const float STRIDE = 0.6680000000000064f;
    public readonly Vector3 STRIDE_X = new Vector3(STRIDE, 0.0f, 0.0f);
    public readonly Vector3 STRIDE_Y = new Vector3(0.0f, STRIDE, 0.0f);
    public const float UPPER_GRID_X = -STRIDE/2 - STRIDE*3;
    public const float UPPER_GRID_Y = STRIDE/2 + STRIDE;
    const float LOWER_SPAWNPOINT_Y = UPPER_GRID_Y + STRIDE/2;
    const float HIGHER_SPAWNPOINT_Y = 2 + 7 * STRIDE;
    GameObject[,] candies = new GameObject[GRID_SIZE, GRID_SIZE];
    

    public Action<bool, (int i, int j), int>  RegenTechnique;

    // Start is called before the first frame update
    void Start() 
    {
        colors = new GameObject[]{ p_Red, p_Blue, p_Yellow, p_Purple, p_Green};
        Time.timeScale = 1;

        InitGrid();

        RegenTechnique = level == 1 ? LevelOneRegen : LevelTwoRegen;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Inits the first screen with uniform random distribution
    // while preventing having a 3-match at the beginning
    void InitGrid()
    {
        Vector3 spawnPosition = new Vector3(UPPER_GRID_X, LOWER_SPAWNPOINT_Y, 0);

        for (int i = 0; i < GRID_SIZE; i++)
        {
            for (int j = 0; j < GRID_SIZE; j++)
            {
                GameObject prefab = RandomUniformCandyChoice();
                int count = 100;
                while (IsInvalidColor(prefab, i, j))
                {
                    prefab = RandomUniformCandyChoice();
                    if (--count == 0) 
                    {
                        // Debug.Log("This morning, I choose the coffee");
                        break;
                    }
                }

                Vector3 position = spawnPosition + j * STRIDE_X + i * STRIDE_Y;
                GameObject go = Instantiate(prefab, position, Quaternion.identity);

                MovingBox box = go.GetComponent<MovingBox>();
                box.SetThreshold(i, j);
                box.Refer(this);
                
                candies[i, j] = go;
            }
        }
    }

    bool IsInvalidColor(GameObject prefab, int i, int j)
    {
        bool left_has_3match = (j > 1 && prefab.tag == candies[i, j-1].tag && prefab.tag == candies[i, j-2].tag);
        bool down_has_3match = (i > 1 && prefab.tag == candies[i-1, j].tag && prefab.tag == candies[i-2, j].tag);

        return left_has_3match || down_has_3match;
    }

    (int i, int j) selection1;
    (int i, int j) selection2;
    int selected;
    public IEnumerator CandySelect(int i, int j)
    {
        if (selected == 0)
        {
            selection1.i = i;
            selection1.j = j;
            // TODO: focus square
            selected = 1;
        }
        else if (selected == 1)
        {
            // Selects a second one
            selection2.i = i;
            selection2.j = j;

            // Check adjacent
            int dx = Math.Abs(selection1.i - selection2.i);
            int dy = Math.Abs(selection1.j - selection2.j);
            if (dx + dy == 1)
            {
                sesh.DecrementMovesLeft();
                yield return StartCoroutine(Checkswap());
                // Check if can cascade
                if (HasNulls()) 
                {
                    yield return StartCoroutine(Cascade(0));
                }

                sesh.EndGameQuestionMark();
            }
            
            selected = 0;

            // TODO: unfocus square
        }
    }

    IEnumerator Checkswap()
    {
        GameObject candy1 = candies[selection1.i, selection1.j];
        GameObject candy2 = candies[selection2.i, selection2.j];

        Swap(candy1, candy2, selection1, selection2);

        // check if can match
        List<GameObject> match1 = MatchAroundSelection(selection1);
        List<GameObject> match2 = MatchAroundSelection(selection2);
        bool matched1 = match1.Count > 2;
        bool matched2 = match2.Count > 2;

        yield return StartCoroutine(MatchTwoSelectionsWait(match1, match2));
        
        if (!matched1 && !matched2)
        {
            // Let them see the swap
            StartCoroutine(SwapWait(candy1, candy2));
        }
    }

    IEnumerator Cascade(int depth)
    {
        // Debug.Log("Niagara falls");
        HashSet<int> columns = new HashSet<int>();

        for (int j = 0; j < GRID_SIZE; j++)
        {
            for (int i = 0; i < GRID_SIZE; i++)
            {
                // column i has a null candy
                if (candies[i,j] == null)
                {
                    columns.Add(j);

                    int emptyToFill = i;
                    int lookahead = 1;
                    while(i+lookahead < GRID_SIZE)
                    {
                        if (candies[i+lookahead, j] != null)
                        {
                            MovingBox candy = candies[i+lookahead, j].GetComponent<MovingBox>();
                            candy.SetNewThreshold(emptyToFill);
                            candies[emptyToFill, j] = candies[i+lookahead, j];
                            candies[i+lookahead, j] = null;
                            emptyToFill++;
                        }
                        else {
                            lookahead++;
                        }
                    }


                    // set decrement threshold
                    // move in array
                    // apply null for the rest

                    // Regen matched candies according to level
                    // This only fills the blanks on a column
                    int valley = GRID_SIZE - emptyToFill;
                    RegenTechnique(valley >= 3, (emptyToFill, j), valley);

                    break;
                }
            }
        }

        // Now the grid is filled
        // Check if new matches in affected columns
        HashSet<GameObject> pool = new HashSet<GameObject>();
        List<string> colorMatches = new List<string>();
        int numberOfMatches = 0;
        foreach (int j in columns)
        {
            for (int i = 0; i < GRID_SIZE; i++)
            {
                List<GameObject> match = MatchAroundSelection((i,j));
                bool matched = match.Count > 2;
                if (matched && !match.Any(pool.Contains))
                {
                    colorMatches.Add(match[0].tag);
                    pool.UnionWith(match);
                    numberOfMatches++;
                }
            }
        }

        if (pool.Count > 0)
        {
            yield return StartCoroutine(MatchWait(pool.ToList()));
            // Add to score like this:
            // depth 1:   n * 100 * 1  
            // depth 2: + m * 100 * 2
            // depth 2: + m * 100 * 4
            // score =  ¯¯¯¯¯¯¯¯¯¯¯¯¯
            int scale = (int)Math.Pow(1.5, depth);
            sesh.AddToScore(numberOfMatches * pointsPerMatch * (scale > 8 ? 8 : scale));
            foreach(string s in colorMatches) 
                sesh.DecrementCount(s);

            if (!sesh.EndGameQuestionMark())
            {
                StartCoroutine(Cascade(depth+1));
            }
        }
    }

    List<GameObject> MatchAroundSelection((int i, int j)selection)
    {
        List<GameObject> sidelist = new List<GameObject>();
        List<GameObject> vertlist = new List<GameObject>();

        int i = selection.i;
        int j = selection.j;

        GameObject selectedCandy = candies[selection.i, selection.j];
        sidelist.Add(selectedCandy);
        vertlist.Add(selectedCandy);

        int l = 1;
        int r = 1;
        int u = 1;
        int d = 1;

        bool stillLookingUp = true;
        bool stillLookingDown = true;
        bool stillLookingLeft = true;
        bool stillLookingRight = true;

        while (stillLookingLeft || stillLookingRight || stillLookingDown || stillLookingUp)
        {
            if (stillLookingLeft && (j - l >= 0 && selectedCandy.tag == candies[i, j-l]?.tag))
            {
                sidelist.Add(candies[i, j-l]);
                l++;
            } else {
                stillLookingLeft = false;
            }
            if (stillLookingRight && (j + r < GRID_SIZE && selectedCandy.tag == candies[i, j+r]?.tag))
            {
                sidelist.Add(candies[i, j+r]);
                r++;
            } else {
                stillLookingRight = false;
            }
            if (stillLookingUp && (i + u < GRID_SIZE && selectedCandy.tag == candies[i+u, j]?.tag))
            {
                vertlist.Add(candies[i+u, j]);
                u++;
            } else {
                stillLookingUp = false;
            }
            if (stillLookingDown && (i - d >= 0 && selectedCandy.tag == candies[i-d, j]?.tag))
            {
                vertlist.Add(candies[i-d, j]);
                d++;
            } else {
                stillLookingDown = false;
            }
        }

        if (sidelist.Count > 2 && vertlist.Count > 2) 
        {
            sidelist.AddRange(vertlist);
            return sidelist;
        }
        if (sidelist.Count > 2) return sidelist;
        return vertlist;
    }

    void Swap(GameObject candy1, GameObject candy2, (int i, int j)selection1, (int i, int j)selection2)
    {
        Vector3 candy1Position = candy1.transform.position;
        Vector3 candy2Position = candy2.transform.position;

        candy1.transform.position = new Vector3(1000,1000,0);

        // Swap thresholds
        candy2.GetComponent<MovingBox>().SetThreshold(selection1.i, selection1.j);
        candy1.GetComponent<MovingBox>().SetThreshold(selection2.i, selection2.j);

        // Swap in array
        candies[selection1.i, selection1.j] = candy2;
        candies[selection2.i, selection2.j] = candy1;

        // Swap positions
        candy2.transform.position = candy1Position;
        candy1.transform.position = candy2Position;
    }

    void LevelOneRegen(bool vertical, (int i, int j) empty, int valley)
    {
        List<GameObject> fillers = new List<GameObject>();
        if (empty.i == 0) fillers.Add(RandomUniformCandyChoice());

        for (int y = fillers.Count; y < valley; y++)
        {
            GameObject under;
            if (fillers.Count == 0) under = CandyPrefabFromTag(candies[empty.i - 1, empty.j].tag);
            else under = fillers[fillers.Count - 1];

            // vertical match
            if (vertical)
            {
                int weight;
                if (fillers.Count < 2) weight = 40;
                else weight = 60;

                fillers.Add(WeightedCandyChoice(under.tag, weight));
            }
            // horizontal
            else 
            {
                fillers.Add(WeightedCandyChoice(under.tag, 60));
            }
        }

        // We have the new blocks for this column
        int added = 0;
        foreach (GameObject prefab in fillers)
        {
            Vector3 spawnPosition = new Vector3(UPPER_GRID_X, LOWER_SPAWNPOINT_Y, 0);

            Vector3 position = spawnPosition + empty.j * STRIDE_X + added * STRIDE_Y;
            GameObject go = Instantiate(prefab, position, Quaternion.identity);
            candies[empty.i, empty.j] = go;

            MovingBox box = go.GetComponent<MovingBox>();
            box.SetThreshold(empty.i, empty.j);
            box.Refer(this);

            empty.i++;
            added++;
        }
    }

    void LevelTwoRegen(bool vertical, (int i, int j) empty, int valley)
    {
        List<GameObject> fillers = new List<GameObject>();
        if (empty.i == 0) fillers.Add(RandomUniformCandyChoice());

        int currentToFill = empty.i;

        for (int y = fillers.Count; y < valley; y++)
        {
            int red = 1;
            int blue = 1;
            int yellow = 1;
            int purple = 1; 
            int green = 1;

            GameObject under;
            if (fillers.Count == 0) under = CandyPrefabFromTag(candies[empty.i - 1, empty.j].tag);
            else under = fillers[fillers.Count - 1];

            // Get all surrounding tiles
            List<GameObject> around = new List<GameObject>();
            around.Add(under);

            // Goes three times, for (-1,-1) (-1,0) (-1,1) and (1,-1) (1,0) (1,1)
            for (int level = -1; level < 2; level++)
            {
                // If has a left neighbour column and within height of grid
                bool hasLeftColumn  = empty.j > 0;
                bool hasRightColumn = empty.j < GRID_SIZE - 1;
                bool isWithinHeight = currentToFill + level > 0 && currentToFill + level < GRID_SIZE;

                if (hasLeftColumn && isWithinHeight && candies[currentToFill+level, empty.j-1] != null)
                    around.Add(candies[currentToFill+level, empty.j-1]);
                // If has a right neighbour column and within height of grid
                if (hasRightColumn && isWithinHeight && candies[currentToFill+level, empty.j+1] != null)
                    around.Add(candies[currentToFill+level, empty.j+1]);
            }
            currentToFill++;

            // Iterate through surrounding tiles and add accordingly
            // The division is not necessary at the end because it is handled in the choice()
            foreach (GameObject go in around)
            {
                switch(go.tag)
                {
                    case "Red": red++; break;
                    case "Blue": blue++; break;
                    case "Purple": purple++; break;
                    case "Yellow": yellow++; break;
                    case "Green": green++; break;
                }
            }

            fillers.Add(WeightedCandyChoice(red, blue, yellow, purple, green));

        }


        // We have the new blocks for this column
        int added = 0;
        foreach (GameObject prefab in fillers)
        {
            Vector3 spawnPosition = new Vector3(UPPER_GRID_X, LOWER_SPAWNPOINT_Y, 0);

            Vector3 position = spawnPosition + empty.j * STRIDE_X + added * STRIDE_Y;
            GameObject go = Instantiate(prefab, position, Quaternion.identity);
            candies[empty.i, empty.j] = go;

            MovingBox box = go.GetComponent<MovingBox>();
            box.SetThreshold(empty.i, empty.j);
            box.Refer(this);

            empty.i++;
            added++;
        }
    }

    bool HasNulls()
    {
        foreach (GameObject candy in candies)
        {
            if (candy == null) return true;
        }
        return false;
    }

    GameObject CandyPrefabFromTag(string color)
    {
        switch (color)
        {
            case "Red": return p_Red;
            case "Blue": return p_Blue;
            case "Purple": return p_Purple;
            case "Yellow": return p_Yellow;
            case "Green": return p_Green;
        }
        return null;
    }

    GameObject RandomUniformCandyChoice()
    {
        return colors[UnityEngine.Random.Range(0,5)];
    }

    IEnumerator SwapWait(GameObject candy1, GameObject candy2)
    {
        yield return new WaitForSeconds(0.5f);

        Swap(candy1, candy2, selection2, selection1);
    }

    IEnumerator MatchTwoSelectionsWait(List<GameObject> match1, List<GameObject> match2)
    {
        yield return new WaitForSeconds(0.5f);
        
        int numberOfMatches = 0;
        if (match1.Count > 2) 
        {
            numberOfMatches++;
            BanishCandies(match1);
            sesh.DecrementCount(match1[0].tag);
        }
        if (match2.Count > 2) 
        {
            numberOfMatches++;
            BanishCandies(match2);
            sesh.DecrementCount(match2[0].tag);
        }

        if (numberOfMatches > 0) 
        {
            plopSFX.Play();
            cascadeSFX.Play();
        }

        sesh.AddToScore(numberOfMatches * pointsPerMatch);
        sesh.EndGameQuestionMark();

    }

    void BanishCandies(List<GameObject> match)
    {
        foreach (GameObject go in match)
        {
            var box = go.GetComponent<MovingBox>();
            candies[box.i,box.j] = null;
            Destroy(go);
        }
    }

    IEnumerator MatchWait(List<GameObject> match)
    {
        yield return new WaitForSeconds(1f);

        plopSFX.Play();
        BanishCandies(match);
        cascadeSFX.Play();
    }

    GameObject WeightedCandyChoice(string color, int weight)
    {
        int equal   = (100 - weight)/(colors.Length-1);
        int red     = equal;
        int blue    = equal;
        int purple  = equal;
        int yellow  = equal;
        int green   = equal;  
        switch (color)
        {
            case "Red": red = weight; break;
            case "Blue": blue = weight; break;
            case "Purple": purple = weight; break;
            case "Yellow": yellow = weight; break;
            case "Green": green = weight; break;
        }

        return WeightedCandyChoice(red, blue, yellow, purple, green);
    }

    GameObject WeightedCandyChoice(int red, int blue, int yellow, int purple, int green)
    {
        int choice = UnityEngine.Random.Range(1,1+red+blue+yellow+purple+green);
        int cumul = red;
        if (choice <= cumul) return p_Red;         
        cumul += blue;
        if (choice <= cumul) return p_Blue;        
        cumul += yellow;
        if (choice <= cumul) return p_Yellow;      
        cumul += purple;
        if (choice <= cumul) return p_Purple;      
        return p_Green;
    }

}
