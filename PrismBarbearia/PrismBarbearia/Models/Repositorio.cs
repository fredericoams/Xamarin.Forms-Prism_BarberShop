﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;

namespace PrismBarbearia.Models
{
    class Repositorio
    {

        public async Task<List<Servicos>> GetServicos()
        {
            List<Servicos> TodosServicos;
            var URLWebAPI = "http://demos.ticapacitacion.com/cats";
           // using (var Client = new System.Net.Http.HttpClient())
            {
                var JSON = await Client.GetStringAsync(URLWebAPI);
                TodosServicos= Newtonsoft.Json.JsonConvert.DeserializeObject<List<Servicos>>(JSON);
            }
            return TodosServicos;
        }




    }
}
