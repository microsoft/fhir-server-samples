// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace FhirDashboard.Models
{
    public class PatientRecord
    {
        public Hl7.Fhir.Model.Patient Patient { get; set; }

        public List<Hl7.Fhir.Model.Observation> Observations { get; set; }

        public List<Hl7.Fhir.Model.Encounter> Encounters { get; set; }
    }
}