using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    [SerializeField] private Transform rowContainer;
    [SerializeField] private PlayerStatusUI rowPrefab;

    private readonly Dictionary<ulong, PlayerStatusUI> rows = new();
    private readonly Dictionary<ulong, NetworkStats> players = new();

    public void RegisterPlayer(NetworkStats player)
    {
        ulong id = player.OwnerClientId;

        if (players.ContainsKey(id))
            return;

        players.Add(id, player);

        PlayerStatusUI row = Instantiate(rowPrefab, rowContainer);
        rows.Add(id, row);

        player.CurrentHealth.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);
        player.Lives.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);
        player.Score.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);
        player.PlayerName.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);

        RefreshPlayer(id);
    }

    public void UnregisterPlayer(ulong id)
    {
        if (!players.ContainsKey(id))
            return;

        players.Remove(id);

        if (rows.TryGetValue(id, out PlayerStatusUI row))
        {
            Destroy(row.gameObject);
            rows.Remove(id);
        }

        SortRows();
    }

    private void RefreshPlayer(ulong id)
    {
        if (!players.TryGetValue(id, out NetworkStats player))
            return;

        if (!rows.TryGetValue(id, out PlayerStatusUI row))
            return;

        row.UpdateContent(player.DisplayName, (int)player.CurrentHealth.Value, player.Lives.Value, player.Score.Value);

        SortRows();
    }

    private void SortRows()
    {
        List<ulong> ranking = players
            .OrderByDescending(p => p.Value.Score.Value)
            .ThenByDescending(p => p.Value.Lives.Value)
            .Select(p => p.Key)
            .ToList();

        for (int i = 0; i < ranking.Count; i++)
        {
            if (rows.TryGetValue(ranking[i], out PlayerStatusUI row))
            {
                row.transform.SetSiblingIndex(i);
            }
        }
    }
}