﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class StartEndTimeUIElementController : MonoBehaviour
{
    private const string k_passedEndDateMessage = "We're finished!";
    
    [SerializeField] private float _updateTimeEvery = 1f;
    [SerializeField] private Text _startText;
    [SerializeField] private Text _endText;
    [SerializeField] private Text _timeLeftText;

    private DateTime _startDate;
    private DateTime _endDate;
    void Start()
    {
        StartCoroutine(UpdateTimesCoroutine());
    }

    public void SetDates(DateTime startDate, DateTime endDate)
    {
        this._startDate = startDate;
        this._endDate = endDate;
        RefreshTimes();
    }

    private IEnumerator UpdateTimesCoroutine()
    {
        RefreshTimes();
        yield return new WaitForSeconds(_updateTimeEvery);
    }

    private string FormatDate(DateTime date)
    {
        return date.ToString("dd MMMM");
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        return ToLongString(timeSpan) + " left";
    }
    
    private void RefreshTimes()
    {
        _startText.text = FormatDate(_startDate);
        _endText.text = FormatDate(_endDate);

        var now = DateTime.Now;
        var timeLeft = String.Empty;
        
        if (now > _endDate)
        {
            timeLeft = k_passedEndDateMessage;
        }
        else
        {
            var difference = _endDate - now;
            timeLeft = FormatTimeSpan(difference);
        }

        _timeLeftText.text = timeLeft;
    }
    
    private string ToLongString(TimeSpan time)
    {
        string output = String.Empty;

        if (time.Days > 0)
        {
            output = $"<b>{time.Days}</b> day{S(time.Days)}";
        }
        else
        {
            output = $"<b>{time.Hours}</b> hour{S(time.Hours)}";
        }
        return output.Trim();
    }

    private string S(int number)
    {
        if (number == 1) return "";
        else return "s";
    }
}