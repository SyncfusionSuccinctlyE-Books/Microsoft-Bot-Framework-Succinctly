using AirfareAlertBot.Controllers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Threading.Tasks;

namespace AirfareAlertBot
{
    // This is the FormFlow class that handles
    // the conversation with the user, in order to get
    // the flight details the user is interested in
    [Serializable]
    public class TravelDetails
    {
        // Ask the user for the point of origin for the trip
        [Prompt(GatherQuestions.cStrGatherQOrigin)]
        public string OriginIata;

        // Ask the user for the point of destination for the trip
        [Prompt(GatherQuestions.cStrGatherQDestination)]
        public string DestinationIata;

        // Ask the user for the outbound trip date
        [Prompt(GatherQuestions.cStrGatherQOutboundDate)]
        public string OutboundDate;

        // Ask the user for the inbound (return) trip date
        // (if applicable - if it is not 'one way'
        [Prompt(GatherQuestions.cStrGatherQInboundDate)]
        [Optional]
        public string InboundDate;

        // Ask the user for the number of passengers
        [Prompt(GatherQuestions.cStrGatherQNumPassengers)]
        public string NumPassengers;

        // Ask the user if the flight is direct
        [Prompt(GatherQuestions.cStrGatherProcessDirect)]
        public string Direct;

        // Ask the user if the flight is to be followed
        // (check for price changes)
        [Prompt(GatherQuestions.cStrGatherProcessFollow)]
        public string Follow;

        // FormFlow main method, which is responsible for creating
        // the conversation dialog with the user and validating each
        // response
        public static IForm<TravelDetails> BuildForm()
        {
            // Once all the user responses have been gathered the
            // send a response back that the request is being 
            // processed
            OnCompletionAsyncDelegate<TravelDetails> processOrder = async (context, state) =>
            {
                await context.PostAsync(GatherQuestions.cStrGatherProcessingReq);
            };

            // FormFlow object that gathers and handles user responses
            var f = new FormBuilder<TravelDetails>()
                    .Message(GatherQuestions.cStrGatherValidData)
                    .Field(nameof(OriginIata),
                    // Validates the point of origin submitted
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            return FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateAirport(state, Data.currentText, false));
                        });
                    })
                    .Message("{OriginIata} selected")

                    .Field(nameof(DestinationIata),
                    // Validates the point of destination submitted
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            return FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateAirport(state, Data.currentText, true));
                        });
                    })
                    .Message("{DestinationIata} selected")

                    .Field(nameof(OutboundDate),
                    // Validates the outbound travel date submitted
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            return FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateDate(state, Data.currentText, false));
                        });
                    })
                    .Message("{OutboundDate} selected")

                    .Field(nameof(InboundDate),
                    // Validates the inbound travel date submitted
                    // (or if it a one way trip)
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            return FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateDate(state, Data.currentText, true));
                        });
                    })
                    .Message("{InboundDate} selected")

                    .Field(nameof(NumPassengers),
                    // Validates the number of passengers submitted for the trip
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            return FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateNumPassengers(state, Data.currentText));
                        });
                    })
                    .Message("{NumPassengers} selected")

                    .Field(nameof(Direct),
                    // Validates if the trip is direct or not
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            return FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateDirect(state, Data.currentText));
                        });
                    })
                    .Message("{Direct} selected")

                    .Field(nameof(Follow),
                    // Validates if the user has submitted the flight to be
                    // followed for price changes
                    validate: async (state, value) =>
                    {
                        return await Task.Run(() =>
                        {
                            Data.currentText = value.ToString();

                            ValidateResult res = FlightFlow.CheckValidateResult(
                                FlightFlow.ValidateFollow(state, Data.currentText));

                            FlightFlow.AssignStateToFlightData(res, state);

                            return res;
                        });
                    })
                    .Message("{Follow} selected")

                    // When all the data has been gathered from the user...
                    .OnCompletion(processOrder)
                    .Build();

            return f;
        }
    }
}