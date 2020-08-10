﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using Random = System.Random;

public class CoinSpawnController: MonoBehaviour, ISaveLoadable<RawCoinSpawnControllerData>
{
    public sealed class DonationEventData
    {
        public string Name;
        public string Message;
        public string ProfileURL;
        public float Amount;
        public bool ShouldShow;
    }
    
    public delegate Vector3 CoinSpawnPositionProvider();
    private const int k_populateBoardWaves = 20;
    private const float k_populateBoardDelay = 0;
    
    private Settings _settings;
    private Coin.Factory _coinFactory;
    private BoardController _boardController;
    private PusherController _pusherController;
    private CampaignModel _campaignModel;

    private List<Coin> _coins = new List<Coin>();
    private List<Coin> _coinPrefabsSortedByValue = new List<Coin>();

    public event Action<DonationEventData> OnDonationMade;

    private bool _isPopulating = false;
    
    public float ValueOnBoard
    {
        get
        {
            return _coins.Sum(coin => coin.value);
        }
    }
    
    [Serializable]
    public sealed class Settings
    {
        public List<Coin> coinPrefabs;
        public Vector3 randomCoinDropOffsetFromCenter;
        public float distanceBetweenCoins;
        public float coinRadius;
        public bool populateOnStart;
    }

    [Inject]
    public void Construct(Settings settings, Coin.Factory coinFactory, BoardController boardController, PusherController pusherController, CampaignModel campaignModel)
    {
        _settings = settings;
        _coinFactory = coinFactory;
        _boardController = boardController;
        _pusherController = pusherController;
        _campaignModel = campaignModel;
        ProcessCoinPrefabs();

        Coin.OnCoinCollected += OnCoinCollected;
    }

    private void OnCoinCollected(Coin coin)
    {
        _coins.Remove(coin);
    }
    
    private void ProcessCoinPrefabs()
    {
        _coinPrefabsSortedByValue = _settings.coinPrefabs.OrderByDescending(coin => coin.value).ToList();
        Debug.Assert(!_coinPrefabsSortedByValue.Any((coin) => coin.value == 0), "Coin has zero value");
    }

    public void PopulateBoard(float value)
    {
        if (_isPopulating) return;
        _isPopulating = true;

        StartCoroutine(PopulateBoardCoroutine(value));
    }

    private IEnumerator PopulateBoardCoroutine(float value)
    {
        float valuePerWave = value / k_populateBoardWaves;
        for (int i = 0; i < k_populateBoardWaves; i++)
        {
            QueueDonation("Backlog", "", GetRandomTestinURL(), valuePerWave, _boardController.GetRandomPopulationPosition, false);
            yield return new WaitForSeconds(k_populateBoardDelay);
        }
        
        _isPopulating = false;
        Debug.Log("Populuated, value on board: " + ValueOnBoard);
    }

    public void ClearBoard()
    {
        foreach (Coin coin in _coins)
        {
            coin.Destroy();
        }
        
        _coins.Clear();
    }

    private Vector3 GetCoinDropPosition()
    {
        Vector3 offset = Vector3.Lerp(-_settings.randomCoinDropOffsetFromCenter,
            _settings.randomCoinDropOffsetFromCenter, UnityEngine.Random.value);

        return _boardController.CoinSpawnPosition + offset;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            QueueDonation("Test Donation", "Test Message", GetRandomTestinURL(), 3.99f);
        }
    }

    public void QueueDonation(string name, string message, string profileURL, float amount, CoinSpawnPositionProvider positionProvider, bool shouldShow = true)
    {
        Debug.Log($"Queuing donation, name: {name}, message: {message}, amount: {amount}");
        var coins = BreakDownBalanceIntoCoins(amount);

        foreach (Coin coin in coins)
        {
            var spawnedCoin = InstansiateCoinFromPrefab(coin);
            spawnedCoin.transform.position = positionProvider();
            spawnedCoin.ApplyMarking(profileURL);
        }

        var donationEventData = new DonationEventData
        {
            Name = name,
            Message = message,
            ProfileURL = profileURL,
            Amount = amount,
            ShouldShow = shouldShow,
        };
        
        OnDonationMade?.Invoke(donationEventData);
    }

    private Coin InstansiateCoinFromPrefab(Coin prefab)
    {
        var newCoin = _coinFactory.Create(prefab);
        _coins.Add(newCoin);

        return newCoin;
    }

    private string GetRandomTestinURL()
    {
        string[] urls =
        {
            "https://pbs.twimg.com/profile_images/1264943635365277697/eSfno0BN_400x400.jpg"
            /*
            "https://cdn.discordapp.com/avatars/368576468093239296/8772431a9e7919ee1f54fc165db78f6d.png?size=128",
            "https://cdn.discordapp.com/avatars/295009627538718722/1ba079588b0443850b152acb27547768.png?size=128",
            "https://cdn.discordapp.com/avatars/295970689335296000/0e644b61b01b50fff39c131b92c2f05c.png?size=128",
            "https://cdn.discordapp.com/avatars/489281810061066250/632956c86b41494093d35966d406b2b0.png?size=128",
            "https://cdn.discordapp.com/avatars/129300349927424001/782a4948ff7e5e2e984d1b224d125640.png?size=128",
            "https://cdn.discordapp.com/avatars/178464497336320000/218d140830b65726e91968e92540cb69.png?size=128",
            "https://cdn.discordapp.com/avatars/328123626174021634/b2c7466676198ee9d0ccda3e8582ade0.png?size=128",
            "https://cdn.discordapp.com/avatars/216145046347448320/35036ca3017da2d6ca5e4975db1f1015.png?size=128"*/
            
            
            
            
        };

        return urls[UnityEngine.Random.Range(0, urls.Length)];
    }

    public void QueueDonation(string name, string message, string profileURL, float amount, bool shouldShow = true)
    {
        QueueDonation(name, message, profileURL, amount, GetCoinDropPosition, shouldShow);
    }
    
    private List<Coin> BreakDownBalanceIntoCoins(float amount)
    {
        var results = new List<Coin>();
        
        while (amount > 0)
        {
            var candidates = _coinPrefabsSortedByValue.Where((cn) => cn.value <= amount);
            if (!candidates.Any()) break;

            var coin = candidates.First();
            results.Add(coin);
            amount -= coin.value;
        }

        return results;
    }

    private Coin GetCoinPrefabFromValue(float value)
    {
        return _coinPrefabsSortedByValue.FirstOrDefault((coin) => Mathf.Approximately(value, coin.value));
    }

    public void Load(RawCoinSpawnControllerData data)
    {
        ClearBoard();

        if (data.coins == null)
        {
            return;
        }
        
        foreach (var rawCoinData in data.coins)
        {
            var prefab = GetCoinPrefabFromValue(rawCoinData.value);
            Debug.Assert(prefab != null, $"Can't find prefab with value of {rawCoinData.value}");

            var coin = InstansiateCoinFromPrefab(prefab);
            coin.Load(rawCoinData);
            coin.ApplyMarking();
        }
        
        Debug.Log($"Loaded {data.coins.Count} from disk");
    }

    public RawCoinSpawnControllerData Save()
    {
        var data = new RawCoinSpawnControllerData();

        foreach (Coin coin in _coins)
        {
            var rawCoinData = coin.Save();
            data.coins.Add(rawCoinData);
        }

        return data;
    }
}

public class RawCoinSpawnControllerData
{
    public List<RawCoinData> coins = new List<RawCoinData>();
}
