﻿using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace APL_Project_Client.Model;
public class Dipendente
{ //Farlo come classe astratta, poi dividere in Utente e Collega
    public string nome;
    public string cognome;
    public string email;

    // Lista di richieste di ferie accettate.
    private List<Ferie> listHolidaysAccepted;
    // Lista di richieste di ferie in attesa.
    private List<Ferie> listRequestPending;
    // Lista di richiese di ferie rifiutate.
    private List<Ferie> listHolidaysRefused;

    public event EventHandler<List<DateTime>> HolidaysReceived;
    public event EventHandler<List<Ferie>> RequestHolidaysUpdated;
    public Dipendente(string nome, string cognome, string email)
	{
        this.nome = nome;
        this.cognome = cognome; 
        this.email = email;
        listRequestPending = new List<Ferie>();
        listHolidaysAccepted= new List<Ferie>();
        listHolidaysRefused = new List<Ferie>();

    }

    private List<DateTime> getHolidaysDays()
    {
        List<DateTime> d = new List<DateTime>();
        if(listHolidaysAccepted != null) 
        {   
            foreach( Ferie holiday in listHolidaysAccepted )
            {
                d.Add(holiday.date);
            }
        
        }
        return d;
    
    } 

    public async Task<bool> fetchHolidays()
    {
        try
        {
            HttpClient client = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("http://localhost:9000/api/getHolidays");
            uriBuilder.Query = "email=" + email;
            HttpResponseMessage response = await client.GetAsync(uriBuilder.ToString());
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync(); 
                dynamic json = JsonConvert.DeserializeObject(content);

                foreach(var holiday in json)
                {
                    if (holiday.type == StatoFerie.Richieste)
                    {
                        int day = holiday.day;
                        int month = holiday.month;
                        int year = holiday.year;
                        string motivation = holiday.message;
                        Ferie f = new Ferie(day, month, year, motivation);
                        listRequestPending.Add(f);
                    } 
                    else if(holiday.type == StatoFerie.Accettate)
                    {
                        int day = holiday.day;
                        int month = holiday.month;
                        int year = holiday.year;
                        string motivation = holiday.message;
                        Ferie f = new Ferie(day, month, year, motivation);
                        f.ApprovaFerie();
                        listHolidaysAccepted.Add(f);
                    }
                    else if (holiday.type == StatoFerie.Rifiutate)
                    {
                        int day = holiday.day;
                        int month = holiday.month;
                        int year = holiday.year;
                        string motivation = holiday.message;
                        Ferie f = new Ferie(day, month, year, motivation);
                        f.RifiutaFerie();
                        listHolidaysRefused.Add(f);
                    }
                }

            }
            else
            {

            }

        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show("Errore nella richiesta: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Errore generico: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        if( HolidaysReceived != null)
        {
            HolidaysReceived(this, getHolidaysDays());
        }
        if (RequestHolidaysUpdated != null)
        {
            RequestHolidaysUpdated(this, getHolidaysRequestedAndRefused());
        }
        return true;
    }

    public async Task<bool> sendHolidayRequest(DateTime date)
    {
        HttpResponseMessage response;
        HttpClient client = new HttpClient();
        Dictionary<string, object> parameters = new Dictionary<string, object> { { "email", email }, { "year", date.Year }, { "month", date.Month }, { "day", date.Day }, { "message", "motivation" } };
        string jsonRequest = JsonConvert.SerializeObject(parameters);
        HttpContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
        try
        {
            response = await client.PostAsync("http://localhost:9000/api/insertHoliday", content);
        
        } catch (HttpRequestException ex)
        {
            MessageBox.Show("Errore nella richiesta: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Errore generico: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            string Resultcontent = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(Resultcontent);
            if ((bool)json.result)
            {
                Ferie f = new Ferie(date.Day, date.Month, date.Year, "Motivation");
                listRequestPending.Add(f);
                if (RequestHolidaysUpdated != null)
                {
                    RequestHolidaysUpdated(this, getHolidaysRequestedAndRefused());
                }
                return true;
            }
        }
        else
        {
            MessageBox.Show("Errore nella richiesta", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        return false;
    }

    public bool isGiornoFerie(DateTime date)
    {
        if( listHolidaysAccepted.Count != 0)
        {
        return listHolidaysAccepted.Any(holiday => holiday.date.Year == holiday.date.Year && holiday.date.Month == holiday.date.Month && holiday.date.Day == date.Day);
        }
        return false;
    }
    //Simile a quello sopra
    public bool RequestContainsDate(DateTime date)
    {
        if (listRequestPending.Count != 0)
        {
            return listRequestPending.Any(holiday => holiday.date.Year == holiday.date.Year && holiday.date.Month == holiday.date.Month && holiday.date.Day == date.Day);
        }
        return false;
    }

    public List<Dipendente> getDipendentiPresenti(DateTime date)
    {
        return new List<Dipendente>();
    }

    private List<Ferie> getHolidaysRequestedAndRefused()
    {
        return listRequestPending.OrderBy(f => f.date).Concat(listHolidaysRefused.OrderBy(f => f.date)).ToList();
    }
}
