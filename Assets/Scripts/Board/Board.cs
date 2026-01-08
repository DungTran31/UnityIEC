using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    public int boardSizeX;

    public int boardSizeY;

    public int bottomCells;

    public Cell[,] m_cells;

    private Transform m_root;

    private int m_matchMin;

    public List<Cell> m_bottomCells;
    private Dictionary<Item, Vector2Int> m_originalPositions;

    public Board(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;
        this.bottomCells = gameSettings.BottomCells;

        m_cells = new Cell[boardSizeX, boardSizeY];
        m_originalPositions = new Dictionary<Item, Vector2Int>();

        CreateBoard();
        CreateBottomCells();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }
    }

    private void CreateBottomCells()
    {
        m_bottomCells = new List<Cell>();
        Vector3 origin = new Vector3(-2f, -boardSizeY * 0.5f - 1f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int i = 0; i < bottomCells; i++)
        {
            GameObject go = GameObject.Instantiate(prefabBG);
            go.transform.position = origin + new Vector3(i, 0, 0f);
            go.transform.SetParent(m_root);

            Cell cell = go.GetComponent<Cell>();
            cell.Setup(i, -1);

            m_bottomCells.Add(cell);
        }
    }

    public bool IsBottomAreaFull()
    {
        return m_bottomCells.All(cell => !cell.IsEmpty);
    }
    public bool IsBottomAreaEmpty()
    {
        return !m_bottomCells.Any(cell => !cell.IsEmpty);
    }
    public void AddItemToBottomArea(Item item)
    {
        foreach (var cell in m_bottomCells)
        {
            if (cell.IsEmpty)
            {
                m_originalPositions[item] = new Vector2Int(item.Cell.BoardX, item.Cell.BoardY);
                cell.Assign(item);
                item.View.DOMove(cell.transform.position, 0.3f).OnComplete(() => {
                    item.SetViewPosition(cell.transform.position);
                });
                break;
            }
        }
    }

    public List<Cell> CheckBottomAreaForMatches()
    {
        var matches = new List<Cell>();

        // Lưu danh sách nhóm theo loại item
        var groups = new List<List<Cell>>();

        foreach (var cell in m_bottomCells)
        {
            if (cell.IsEmpty) continue;

            // Kiểm tra nếu cell đã có trong nhóm nào đó chưa
            bool added = false;
            foreach (var group in groups)
            {
                if (group.Count > 0 && cell.IsSameType(group[0]))
                {
                    group.Add(cell);
                    added = true;
                    break;
                }
            }

            // Nếu chưa có nhóm nào phù hợp, tạo nhóm mới
            if (!added)
            {
                groups.Add(new List<Cell> { cell });
            }
        }

        // Chỉ lấy các nhóm có từ 3 cell trở lên
        foreach (var group in groups)
        {
            if (group.Count >= 3)
            {
                matches.AddRange(group);
            }
        }

        return matches;
    }

    public void MoveItemFromBottomToOriginal(Cell bottomCell)
    {
        if (bottomCell.IsEmpty) return;

        var item = bottomCell.Item;
        bottomCell.Free();

        if (m_originalPositions.TryGetValue(item, out Vector2Int originalPosition))
        {
            var originalCell = m_cells[originalPosition.x, originalPosition.y];
            originalCell.Assign(item);
            item.View.DOMove(originalCell.transform.position, 0.3f).OnComplete(() => {
                item.SetViewPosition(originalCell.transform.position);
            });
            m_originalPositions.Remove(item);
        }
    }


    public void ClearBottomArea()
    {
        foreach (var cell in m_bottomCells)
        {
            cell.Free();
        }
    }

    internal void Fill()
    {
        // Lấy danh sách các loại item
        var allTypes = Enum.GetValues(typeof(NormalItem.eNormalType)).Cast<NormalItem.eNormalType>().ToList();
        int totalCells = boardSizeX * boardSizeY;

        // Xác định số lượng item mỗi loại
        int itemsPerType = totalCells / allTypes.Count;
        itemsPerType -= itemsPerType % 3; // Đảm bảo chia hết cho 3
        int remaining = totalCells - (itemsPerType * allTypes.Count); // Số ô còn lại chưa phân bổ

        Dictionary<NormalItem.eNormalType, int> typeCounts = new Dictionary<NormalItem.eNormalType, int>();
        foreach (var type in allTypes)
        {
            typeCounts[type] = itemsPerType;
        }

        // Phân bổ số dư (vẫn đảm bảo chia hết cho 3)
        foreach (var type in allTypes)
        {
            if (remaining >= 3)
            {
                typeCounts[type] += 3;
                remaining -= 3;
            }
        }

        // Tạo danh sách item dựa trên số lượng đã phân bổ
        List<NormalItem> itemList = new List<NormalItem>();
        foreach (var type in typeCounts)
        {
            for (int i = 0; i < type.Value; i++)
            {
                NormalItem item = new NormalItem();
                item.SetType(type.Key);
                itemList.Add(item);
            }
        }

        // Xáo trộn danh sách để ngẫu nhiên
        itemList = itemList.OrderBy(x => UnityEngine.Random.value).ToList();

        // Đưa item vào bảng
        int index = 0;
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = itemList[index++];
                item.SetView();
                item.SetViewRoot(m_root);
                cell.Assign(item);
                cell.ApplyItemPosition(false);
            }
        }
    }



    
}