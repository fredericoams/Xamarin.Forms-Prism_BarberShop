﻿using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;
using Plugin.Connectivity;
using PrismBarbearia.Models;
using PrismBarbearia.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AzureDataService))]

namespace PrismBarbearia.Services
{
    public class AzureDataService
    {

        public MobileServiceClient Client { get; set; } = null;
        public int TimerZone { get; set; }
        IMobileServiceSyncTable<BarberSchedule> scheduleTable;
        IMobileServiceSyncTable<BarberService> serviceTable;

        public async Task Initialize()
        {
            if (Client?.SyncContext?.IsInitialized ?? false)
                return;

            var appUrl = "https://barbearia8ball.azurewebsites.net";

            Client = new MobileServiceClient(appUrl);

            //InitializeDatabase for path
            var path = "syncstore.db";
            path = Path.Combine(MobileServiceClient.DefaultDatabasePath, path);

            //setup our local sqlite store and intialize our table
            var store = new MobileServiceSQLiteStore(path);

            //Define table
            store.DefineTable<BarberSchedule>();
            store.DefineTable<BarberService>();

            //Initialize SyncContext
            await Client.SyncContext.InitializeAsync(store);

            //Get our sync table that will call out to azure
            scheduleTable = Client.GetSyncTable<BarberSchedule>();
            serviceTable = Client.GetSyncTable<BarberService>();
            //scheduleTable = Client.GetSyncTable<BarberSchedule>();
        }

        public async Task SyncSchedule()
        {
            try
            {
                if (!CrossConnectivity.Current.IsConnected)
                    return;

                await scheduleTable.PullAsync("agendamentosFeitos", scheduleTable.CreateQuery());
                await Client.SyncContext.PushAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to sync schedules, that is alright as we have offline capabilities: " + ex);
            }

        }

        public async Task SyncService()
        {
            try
            {
                if (!CrossConnectivity.Current.IsConnected)
                    return;

                await serviceTable.PullAsync("servicosFeitos", serviceTable.CreateQuery());
                await Client.SyncContext.PushAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to sync schedules, that is alright as we have offline capabilities: " + ex);
            }

        }

        public async Task<IEnumerable<BarberSchedule>> GetSchedule()
        {
            await Initialize();
            await SyncSchedule();

            return await scheduleTable.ToEnumerableAsync();
        }

        public async Task<BarberSchedule> AddSchedule(string service, string name, string phoneNumber, string email, string birthday, DateTime dateTime)
        {
            await Initialize();
            DateTime inicioHorarioVerao = new DateTime(DateTime.Now.Year, 11, 4, 0, 0, 0);
            DateTime terminoHorarioVerao = new DateTime(DateTime.Now.Year + 1, 2, 16, 23, 59, 59);

            int depoisinicioHorarioDeVerao = DateTime.Compare(dateTime, inicioHorarioVerao);
            int antesTerminoHorarioDeVerao = DateTime.Compare(terminoHorarioVerao, dateTime);

            if (depoisinicioHorarioDeVerao > 0 && antesTerminoHorarioDeVerao > 0)
                TimerZone = -2;
            else
                TimerZone = -3;

            var schedule = new BarberSchedule
            {
                Service = service,
                Name = name,
                PhoneNumber = phoneNumber,
                Email = email,
                Birthday = birthday,
                DateTime = dateTime.AddHours(TimerZone)
            };
            await scheduleTable.InsertAsync(schedule);
            await SyncSchedule();
            return schedule;
        }

        public async Task<BarberSchedule> RemoveSchedule(string id)
        {
            await Initialize();
            var schedule = new BarberSchedule { Id = id };
            await scheduleTable.DeleteAsync(schedule);
            await SyncSchedule();
            return schedule;
        }

        public async Task<BarberService> AddService(string name, string price)
        {
            await Initialize();
            var service = new BarberService
            {
                ServiceName = name,
                ServicePrice = "R$ " + price
            };
            await serviceTable.InsertAsync(service);
            await SyncService();
            return service;
        }

        public async Task<BarberService> RemoveService(string id)
        {
            await Initialize();
            var service = new BarberService { Id = id };
            await serviceTable.DeleteAsync(service);
            await SyncService();
            return service;
        }
    }
}