// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FhirDashboard.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace FhirDashboard.Controllers
{
    public class PatientController : Controller
    {
        private IConfiguration _configuration;
        private ITokenAcquisition _tokenAcquisition;

        public PatientController(IConfiguration config, ITokenAcquisition tokenAcquisition)
        {
            _configuration = config;
            _tokenAcquisition = tokenAcquisition;
        }

        public async Task<IActionResult> Index()
        {
            var scopes = new string[] { $"{_configuration["FhirServerUrl"].TrimEnd('/')}/.default" };
            string accessToken;
            try
            {
                accessToken = await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(HttpContext, scopes);
            }
            catch (MsalUiRequiredException ex)
            {
                AuthenticationProperties properties = AuthenticationPropertiesBuilder.BuildForIncrementalConsent(scopes, HttpContext, ex);
                return Challenge(properties);
            }

            var client = GetClientAsync(accessToken);
            Bundle result = null;
            List<Patient> patientResults = new List<Patient>();

            try
            {
                if (!string.IsNullOrEmpty(Request.Query["ct"]))
                {
                    string cont = Request.Query["ct"];
                    result = client.Search<Patient>(new string[] { $"ct={cont}" });
                }
                else
                {
                    result = client.Search<Patient>();
                }

                if (result.Entry != null)
                {
                    foreach (var e in result.Entry)
                    {
                        patientResults.Add((Patient)e.Resource);
                    }
                }

                if (result.NextLink != null)
                {
                    ViewData["NextLink"] = result.NextLink.PathAndQuery;
                }
            }
            catch (Exception e)
            {
                ViewData["ErrorMessage"] = e.Message;
            }

            return View(patientResults);
        }

        [HttpGet("/Patient/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var scopes = new string[] { $"{_configuration["FhirServerUrl"].TrimEnd('/')}/.default" };
            string accessToken;
            try
            {
                accessToken = await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(HttpContext, scopes);
            }
            catch (MsalUiRequiredException ex)
            {
                AuthenticationProperties properties = AuthenticationPropertiesBuilder.BuildForIncrementalConsent(scopes, HttpContext, ex);
                return Challenge(properties);
            }

            var client = GetClientAsync(accessToken);
            PatientRecord patientRecord = new PatientRecord();

            try
            {
                var patientResult = client.Search<Patient>(new string[] { $"_id={id}" });
                if ((patientResult.Entry != null) && (patientResult.Entry.Count > 0))
                {
                    patientRecord.Patient = (Patient)patientResult.Entry[0].Resource;
                }

                if (patientRecord.Patient != null)
                {
                    patientRecord.Observations = new List<Observation>();
                    var observationResult = client.Search<Observation>(new string[] { $"subject=Patient/{patientRecord.Patient.Id}" });

                    while (observationResult != null)
                    {
                        foreach (var o in observationResult.Entry)
                        {
                            patientRecord.Observations.Add((Observation)o.Resource);
                        }

                        observationResult = client.Continue(observationResult);
                    }

                    patientRecord.Encounters = new List<Encounter>();
                    var encounterResult = client.Search<Encounter>(new string[] { $"subject=Patient/{patientRecord.Patient.Id}" });

                    while (encounterResult != null)
                    {
                        foreach (var e in encounterResult.Entry)
                        {
                            patientRecord.Encounters.Add((Encounter)e.Resource);
                        }

                        encounterResult = client.Continue(encounterResult);
                    }
                }
            }
            catch (Exception e)
            {
                ViewData["ErrorMessage"] = e.Message;
            }

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { patient = id }));
            var launchContext = Convert.ToBase64String(plainTextBytes);
            ViewData["launchContext"] = HttpUtility.UrlEncode(launchContext);
            ViewData["fhirServerUrl"] = _configuration["FhirServerUrl"];
            return View(patientRecord);
        }

        private FhirClient GetClientAsync(string accessToken)
        {
            var client = new Hl7.Fhir.Rest.FhirClient(_configuration["FhirServerUrl"]);
            client.OnBeforeRequest += (object sender, BeforeRequestEventArgs e) =>
            {
                e.RawRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            };
            client.PreferredFormat = ResourceFormat.Json;
            return client;
        }
    }
}