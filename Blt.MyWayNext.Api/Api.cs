using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Blt.MyWayNext.Bol;
using Blt.MyWayNext.Tool;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;

namespace Blt.MyWayNext.Api
{
    public class MWNextApi
    {
        public async Task<MyWayApiResponse> ImportAnagraficaTemporanea(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            try
            {
                response = await Business.Business.ImportAnagraficaTemporanea(form, name);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;
        }

        public async Task<MyWayApiResponse> ImportAnagraficaTemporaneaIniziativa(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.ImportAnagraficaTemporaneaIniziativa(form, name);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage += ex.Message;

            }

            return response;
        }


        public async Task<MyWayApiResponse> ImportAttivitaCommerciale(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.ImportAttivitaCommerciale(form, name);

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public async Task<MyWayApiResponse> ImportAggiornaAttivitaCommerciale(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.ImportAggiornaAttivitaCommerciale(form, name);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public async Task<MyWayAnagraficheResponse> GetAnagrafiche(string pattern)
        {
            MyWayAnagraficheResponse response = new MyWayAnagraficheResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.GetAnagrafiche(pattern);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public async Task<MyWayIniziativaResponse> GetIniziative(string Anagrafica, string isTemporanea )
        {
            MyWayIniziativaResponse response = new MyWayIniziativaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.GetIniziativeCommerciali(Anagrafica, isTemporanea);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public async Task<MyWayTrattativaResponse> GetTrattative(string codTrattativa)
        {
            MyWayTrattativaResponse response = new MyWayTrattativaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.GetTrattativeCommerciali(codTrattativa);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public async Task<MyWayApiResponse> SetTrattativa(MyWayObjTrattativa trattativa)
        {
            MyWayApiResponse     response = new MyWayApiResponse();
            
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.SetTrattativaCommerciale(trattativa);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public async Task<MyWayApiResponse> PutTrattativa(MyWayObjTrattativa trattativa)
        {
            MyWayTrattativaResponse response = new MyWayTrattativaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                response = await Business.Business.PutTrattativaCommerciale(trattativa);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

    }


}