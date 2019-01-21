// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace FhirDashboard
{
    public static class AgeBirthDayExtension
    {
        public static int AgeYears(this DateTime dob)
        {
            var today = DateTime.Today;

            // Calculate the age.
            var age = today.Year - dob.Year;

            // Go back to the year the person was born in case of a leap year
            if (dob > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}