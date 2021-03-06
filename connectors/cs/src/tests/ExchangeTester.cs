/* Copyright (c) 2008 Google Inc. All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Text;

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.WebDav;
using Google.GCalExchangeSync.Library.Util;

using TZ4Net;

namespace Google.GCalExchangeSync.Tests.Diagnostics
{
    public class ExchangeTester
    {
        public static ExchangeUserDict QueryActiveDirectory(string ldapFilter)
        {
            ExchangeService gw = new ExchangeService(
                ConfigCache.ExchangeServerUrl,
                ConfigCache.ExchangeUserLogin,
                ConfigCache.ExchangeUserPassword );
            return gw.QueryActiveDirectory( ldapFilter );
        }

        public static ExchangeUserDict QueryFreeBusy(string email)
        {
            ExchangeService gw = new ExchangeService(
                ConfigCache.ExchangeServerUrl,
                ConfigCache.ExchangeUserLogin,
                ConfigCache.ExchangeUserPassword );

            DateTimeRange range = new DateTimeRange(
                DateUtil.NowUtc.AddDays(-7),
                DateUtil.NowUtc.AddDays(+7));

            return gw.SearchByEmail( range, email );
        }

        public static void WriteAppointment(
            string email, DateTime appointmentStart )
        {
            DateTime appointmentEnd = appointmentStart.AddHours( 1 );

            ExchangeService gw = new ExchangeService(
                ConfigCache.ExchangeServerUrl,
                ConfigCache.ExchangeAdminLogin,
                ConfigCache.ExchangeAdminPassword );

            ExchangeUserDict users =
                gw.QueryActiveDirectory( string.Format("mail={0}", email ));

            if (users.Count != 0)
            {
                foreach (ExchangeUser user in users.Values)
                {
                    Appointment appt = new Appointment();

                    appt.Subject = "GCalExchangeSync test appt";
                    appt.Location = "test";
                    appt.StartDate = appointmentStart;
                    appt.EndDate = appointmentEnd;
                    appt.MeetingStatus = MeetingStatus.Confirmed;
                    appt.Created = DateUtil.NowUtc;
                    appt.InstanceType = InstanceType.Single;

                    List<Appointment> list = new List<Appointment>();
                    list.Add(appt);

                    gw.Appointments.WriteAppointments(user, list);
                }
            }
            else
            {
                string msg = string.Format("User {0} not found", email);
                throw new Exception(msg);
            }
        }

        public static void WriteFreeBusyMessage( string commonName )
        {
            ExchangeService gw = new ExchangeService(
                ConfigCache.ExchangeFreeBusyServerUrl,
                ConfigCache.ExchangeUserLogin,
                ConfigCache.ExchangeUserPassword );

            SchedulePlusFreeBusyWriter writer =
                new SchedulePlusFreeBusyWriter();

            ExchangeUserDict users = QueryFreeBusy(commonName);
            if (users.Count < 1)
            {
                string msg = string.Format("User {0} not found", commonName);
                throw new Exception(msg);
            }

            if (users.Count > 1)
            {
                string msg = string.Format("More than one user matches {0}", commonName);
                throw new Exception(msg);
            }

            string userFreeBusyUrl = FreeBusyUrl.GenerateUrlFromDN(
                ConfigCache.ExchangeFreeBusyServerUrl,
                users[commonName].LegacyExchangeDN);

            List<string> emtpyList = new List<string>();
            string nowMinus30Days =
                FreeBusyConverter.ConvertToSysTime(DateUtil.NowUtc.AddDays(-30)).ToString();
            string nowPlus60Days =
                FreeBusyConverter.ConvertToSysTime(DateUtil.NowUtc.AddDays(60)).ToString();

            gw.FreeBusy.CreateFreeBusyMessage(userFreeBusyUrl,
                                              commonName,
                                              emtpyList,
                                              emtpyList,
                                              emtpyList,
                                              emtpyList,
                                              nowMinus30Days,
                                              nowPlus60Days);
        }
    }
}
