// -----------------------------------------------------------------------
// <copyright file="SourceFunctions.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Msix.Utils.Logger;
    using Microsoft.OWCUtils.Diagnostics;
    using Microsoft.OWCUtils.FunctionInputHelpers;
    using Microsoft.OWCUtils.HttpRequestHelpers;
    using Microsoft.WinGet.Exists.Core;
    using Microsoft.WinGet.Rebuild.Core;
    using Microsoft.WinGet.RestSource.Functions.Common;
    using Microsoft.WinGet.RestSource.Functions.Constants;
    using Microsoft.WinGet.RestSource.Utils.Common;
    using Microsoft.WinGet.RestSource.Utils.Constants;
    using Microsoft.WinGet.RestSource.Utils.Exceptions;
    using Microsoft.WinGet.RestSource.Utils.Models;
    using Microsoft.WinGet.RestSource.Utils.Models.Errors;
    using Microsoft.WinGet.RestSource.Utils.Models.Schemas;
    using Microsoft.WinGet.RestSource.Utils.ToMoveToUtils;
    using Microsoft.WinGet.RestSource.Utils.Validators;
    using Microsoft.WinGet.Update.Core;
    using ContextAndReferenceInputHelper = Microsoft.WinGet.RestSource.Utils.ToMoveToUtils.ContextAndReferenceInputHelper;

    /// <summary>
    /// This class contains the functions for uploads from and querying data about the github winget-pkgs repository.
    /// </summary>
    public class SourceFunctions
    {
        private readonly HttpClient httpClient;
        private readonly IRebuild rebuildHandler;
        private readonly IUpdate updateHandler;
        private readonly IExists existsHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceFunctions"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Http client factory.</param>
        /// <param name="rebuildHandler">An object of type <see cref="IRebuild"/>.</param>
        /// <param name="updateHandler">An object of type <see cref="IUpdate"/>.</param>
        /// <param name="existsHandler">An object of type <see cref="IExists"/>.</param>
        public SourceFunctions(
            IHttpClientFactory httpClientFactory,
            IRebuild rebuildHandler,
            IUpdate updateHandler,
            IExists existsHandler)
        {
            this.httpClient = httpClientFactory.CreateClient();
            this.rebuildHandler = rebuildHandler;
            this.updateHandler = updateHandler;
            this.existsHandler = existsHandler;
        }

        /// <summary>
        /// Azure function to dispatch source rebuild work.
        /// </summary>
        /// <param name="durableContext">Durable orchestration context.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [FunctionName(FunctionConstants.RebuildOrchestrator)]
        public async Task<SourceResultOutputHelper> RebuildOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext durableContext,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            SourceResultOutputHelper rebuildResult = new SourceResultOutputHelper(SourceResultType.Error);
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();

            ContextAndReferenceInputHelper inputHelper = null;
            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    durableContext.CreateReplaySafeLogger(logger),
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                inputHelper = durableContext.GetInput<ContextAndReferenceInputHelper>();

                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName,
                    executionContext.InvocationId.ToString(),
                    inputHelper.OperationId,
                    null,
                    null);

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", inputHelper.OperationId);
                customDimensions.Add("SASReference", inputHelper.SASReference);

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                Logger.Info($"{loggingContext}RebuildOrchestratorAsync function executed at: {durableContext.CurrentUtcDateTime}");

                // Call Activity function for Rebuild
                Logger.Info($"{loggingContext} Calling Rebuild activity function.");
                rebuildResult = await durableContext.CallActivityAsync<SourceResultOutputHelper>(
                    FunctionConstants.RebuildActivity,
                    inputHelper);

                Logger.Info($"{loggingContext} Rebuild activity function verification result {rebuildResult}.");
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred in RebuildOrchestratorAsync {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
            }
            finally
            {
                if (inputHelper != null)
                {
                    Logger.Info($"{loggingContext}Task result: {rebuildResult}");
                }

                customDimensions.Add("Result", rebuildResult.ToString());

                /* this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
                if (taskResult.OverallResult == ResultType.Error)
                {
                    Geneva.EmitMetric(Metrics.ValidationPipelineError, customDimensions, loggingContext);
                } */
            }

            return rebuildResult;
        }

        /// <summary>
        /// This function provides an API call that will perform the source rebuild.
        /// </summary>
        /// <param name="durableContext">Durable context.</param>
        /// <param name="logger">This is the default ILogger passed in for Azure Functions.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName(FunctionConstants.RebuildActivity)]
        public async Task<SourceResultOutputHelper> RebuildActivityAsync(
            [ActivityTrigger] IDurableActivityContext durableContext,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();
            SourceResultOutputHelper rebuildResult = new SourceResultOutputHelper(SourceResultType.Error);

            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    logger,
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                ContextAndReferenceInputHelper inputHelper = durableContext.GetInput<ContextAndReferenceInputHelper>();

                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName, executionContext.InvocationId.ToString(), inputHelper.OperationId);
                Logger.Info($"{loggingContext}Starting Rebuild processing. Received: {inputHelper}");

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", inputHelper.OperationId);
                customDimensions.Add("SASReference", inputHelper.SASReference);

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                rebuildResult = await this.rebuildHandler.ProcessRebuildRequestAsync(
                    this.httpClient,
                    inputHelper.OperationId,
                    inputHelper.SASReference,
                    inputHelper.ReferenceType,
                    loggingContext);
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred : {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
                throw;
            }
            finally
            {
                customDimensions.Add("Result", rebuildResult.ToString());

                // this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
            }

            return rebuildResult;
        }

        /// <summary>
        /// Rebuild Post Function.
        /// This function supports analyzing an SQLite file and attempting to match the cosmos Db state to the file.
        /// This function involves doing multiple full passes of the database, thus will be very expensive. It should be used
        /// sparingly only for bootstapping catalogs and recovering from significant failures.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="durableClient">Durable client object.</param>
        /// <param name="logger">ILogger.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName(FunctionConstants.RebuildPost)]
        public async Task<IActionResult> RebuildPostAsync(
            [HttpTrigger(AuthorizationLevel.Function, FunctionConstants.FunctionPost, Route = "rebuild")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableClient,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();
            string orchestrationInstanceId;

            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    logger,
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                req.EnableBuffering();

                ContextAndReferenceInputHelper requestData =
                    await RequestBodyHelper.GetRequestDataFromBody<ContextAndReferenceInputHelper>(req.Body, true);
                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName, executionContext.InvocationId.ToString(), requestData.OperationId);

                Logger.Info($"{loggingContext}Starting Rebuild processing. Received: {requestData}");
                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", requestData.OperationId);
                customDimensions.Add("SASReference", requestData.SASReference);

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                ContextAndReferenceInputHelper azureFunctionInputHelper =
                    new ContextAndReferenceInputHelper(
                        requestData.OperationId,
                        requestData.SASReference,
                        requestData.ReferenceType);

                orchestrationInstanceId = await durableClient.StartNewAsync(
                    FunctionConstants.RebuildOrchestrator,
                    input: azureFunctionInputHelper);

                Logger.Info($"{loggingContext}{FunctionConstants.RebuildOrchestrator} " +
                    $"Orchestration instance id:  {orchestrationInstanceId}.");

                customDimensions.Add("Result", "Scheduled Rebuild operations.");
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred : {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
                return new BadRequestObjectResult(new { Name = $"Error: {e}" });
            }
            finally
            {
                // this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
            }

            // This returns information that the client can use to query the status of the running orchestration.
            // We expect the client to poll for results and pull the success/fail of the operation from the output of the status response.
            // We are leveraging the full durable function pre-built infrastructure to offer our API Async.
            return durableClient.CreateCheckStatusResponse(req, orchestrationInstanceId);
        }

        /// <summary>
        /// Azure function to dispatch source update work.
        /// </summary>
        /// <param name="durableContext">Durable orchestration context.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [FunctionName(FunctionConstants.UpdateOrchestrator)]
        public async Task<SourceResultAndCommitsOutputHelper> UpdateOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext durableContext,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            SourceResultAndCommitsOutputHelper updateResult = new SourceResultAndCommitsOutputHelper(SourceResultType.Error);
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();

            ContextAndCommitsInputHelper inputHelper = null;
            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    durableContext.CreateReplaySafeLogger(logger),
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                inputHelper = durableContext.GetInput<ContextAndCommitsInputHelper>();

                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName,
                    executionContext.InvocationId.ToString(),
                    inputHelper.OperationId,
                    null,
                    null);

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", inputHelper.OperationId);
                customDimensions.Add("Commits", string.Join(",", inputHelper.Commits));

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                Logger.Info($"{loggingContext}UpdateOrchestratorAsync function executed at: {durableContext.CurrentUtcDateTime}");

                // Call Activity function for update
                Logger.Info($"{loggingContext} Calling Update activity function.");
                updateResult = await durableContext.CallActivityAsync<SourceResultAndCommitsOutputHelper>(
                    FunctionConstants.UpdateActivity,
                    inputHelper);

                Logger.Info($"{loggingContext} Update activity function result {updateResult}.");
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred in UpdateOrchestratorAsync {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
            }
            finally
            {
                if (inputHelper != null)
                {
                    Logger.Info($"{loggingContext}Task result: {updateResult}");
                }

                customDimensions.Add("Result", updateResult.ToString());

                /*this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
                if (taskResult.OverallResult == ResultType.Error)
                {
                    Geneva.EmitMetric(Metrics.ValidationPipelineError, customDimensions, loggingContext);
                }
                */
            }

            return updateResult;
        }

        /// <summary>
        /// This function provides an API call that will perform the source update.
        /// </summary>
        /// <param name="durableContext">Durable context.</param>
        /// <param name="logger">This is the default ILogger passed in for Azure Functions.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName(FunctionConstants.UpdateActivity)]
        public async Task<SourceResultAndCommitsOutputHelper> UpdateActivityAsync(
            [ActivityTrigger] IDurableActivityContext durableContext,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();
            SourceResultAndCommitsOutputHelper updateResult = new SourceResultAndCommitsOutputHelper(SourceResultType.Error);

            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    logger,
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                ContextAndCommitsInputHelper inputHelper = durableContext.GetInput<ContextAndCommitsInputHelper>();

                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName, executionContext.InvocationId.ToString(), inputHelper.OperationId);
                Logger.Info($"{loggingContext}Starting Update processing. Received: {inputHelper}");

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", inputHelper.OperationId);
                customDimensions.Add("Commits", string.Join(",", inputHelper.Commits));

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                updateResult = await this.updateHandler.ProcessUpdateRequestAsync(
                    this.httpClient,
                    inputHelper.OperationId,
                    inputHelper.Commits,
                    loggingContext);
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred : {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
                throw;
            }
            finally
            {
                customDimensions.Add("Result", updateResult.ToString());

                // this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
            }

            return updateResult;
        }

        /// <summary>
        /// Update Post Function.
        /// This function supports analyzing a list of commits for changes to the repository.
        /// For each change, we trigger the corresponding operation against the rest source implementation to
        /// add, remove, or update an application.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="durableClient">Durable client object.</param>
        /// <param name="logger">ILogger.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName(FunctionConstants.UpdatePost)]
        public async Task<IActionResult> UpdatePostAsync(
            [HttpTrigger(AuthorizationLevel.Function, FunctionConstants.FunctionPost, Route = "update")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableClient,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();
            string orchestrationInstanceId;

            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    logger,
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                req.EnableBuffering();

                ContextAndCommitsInputHelper requestData =
                    await RequestBodyHelper.GetRequestDataFromBody<ContextAndCommitsInputHelper>(req.Body, true);
                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName, executionContext.InvocationId.ToString(), requestData.OperationId);

                Logger.Info($"{loggingContext}Starting Update processing. Received: {requestData}");

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", requestData.OperationId);
                customDimensions.Add("Commits", string.Join(",", requestData.Commits));

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                ContextAndCommitsInputHelper azureFunctionInputHelper =
                    new ContextAndCommitsInputHelper(
                        requestData.OperationId,
                        requestData.Commits);

                orchestrationInstanceId = await durableClient.StartNewAsync(
                    FunctionConstants.UpdateOrchestrator,
                    input: azureFunctionInputHelper);

                Logger.Info($"{loggingContext}{FunctionConstants.UpdateOrchestrator} " +
                    $"Orchestration instance id:  {orchestrationInstanceId}.");

                customDimensions.Add("Result", "Scheduled Update operations.");
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred : {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
                return new BadRequestObjectResult(new { Name = $"Error: {e}" });
            }
            finally
            {
                // this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
            }

            // This returns information that the client can use to query the status of the running orchestration.
            // We expect the client to poll for results and pull the success/fail of the operation from the output of the status response.
            // We are leveraging the full durable function pre-built infrastructure to offer our API Async.
            return durableClient.CreateCheckStatusResponse(req, orchestrationInstanceId);
        }

        /// <summary>
        /// Azure function to dispatch source existence checking on a value.
        /// </summary>
        /// <param name="durableContext">Durable orchestration context.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [FunctionName(FunctionConstants.ExistsOrchestrator)]
        public async Task<ExistsResultAndPackagesOutputHelper> ExistsOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext durableContext,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            ExistsResultAndPackagesOutputHelper existsResult = new ExistsResultAndPackagesOutputHelper(ExistsResultType.Error);
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();

            ContextTypeAndValueInputHelper inputHelper = null;
            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    durableContext.CreateReplaySafeLogger(logger),
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                inputHelper = durableContext.GetInput<ContextTypeAndValueInputHelper>();

                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName,
                    executionContext.InvocationId.ToString(),
                    inputHelper.OperationId,
                    null,
                    null);

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", inputHelper.OperationId);
                customDimensions.Add("Type", inputHelper.Type.ToString());
                customDimensions.Add("Value", inputHelper.Value);

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                Logger.Info($"{loggingContext}ExistsOrchestratorAsync function executed at: {durableContext.CurrentUtcDateTime}");

                // Call Activity function for existence checking.
                Logger.Info($"{loggingContext} Calling Exists activity function.");
                existsResult = await durableContext.CallActivityAsync<ExistsResultAndPackagesOutputHelper>(
                    FunctionConstants.ExistsActivity,
                    inputHelper);

                Logger.Info($"{loggingContext} Exists activity function result {existsResult}.");
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred in ExistsOrchestratorAsync {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
            }
            finally
            {
                if (inputHelper != null)
                {
                    Logger.Info($"{loggingContext}Task result: {existsResult}");
                }

                customDimensions.Add("Result", existsResult.ToString());

                /* this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
                if (existsResult.OverallResult == ExistsResultType.Error)
                {
                    Geneva.EmitMetric(Metrics.ValidationPipelineError, customDimensions, loggingContext);
                }
                */
            }

            return existsResult;
        }

        /// <summary>
        /// This function provides an API call that will perform source existence checking on a value.
        /// </summary>
        /// <param name="durableContext">Durable context.</param>
        /// <param name="logger">This is the default ILogger passed in for Azure Functions.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>IActionResult.</returns>
        /// TODO: return async Task when function is implemented and is actually async.
        [FunctionName(FunctionConstants.ExistsActivity)]
        public ExistsResultAndPackagesOutputHelper ExistsActivityAsync(
            [ActivityTrigger] IDurableActivityContext durableContext,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();
            ExistsResultAndPackagesOutputHelper existsResult = new ExistsResultAndPackagesOutputHelper(ExistsResultType.Error);

            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    logger,
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                ContextTypeAndValueInputHelper inputHelper = durableContext.GetInput<ContextTypeAndValueInputHelper>();

                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName, executionContext.InvocationId.ToString(), inputHelper.OperationId);
                Logger.Info($"{loggingContext}Starting Exists activity processing. Received: {inputHelper}");

                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", inputHelper.OperationId);
                customDimensions.Add("Type", inputHelper.Type.ToString());
                customDimensions.Add("Value", inputHelper.Value);

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                existsResult = this.existsHandler.ProcessExistsRequestAsync(
                    this.httpClient,
                    inputHelper.OperationId,
                    inputHelper.Type,
                    inputHelper.Value,
                    loggingContext);
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred : {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
                throw;
            }
            finally
            {
                customDimensions.Add("Result", existsResult.ToString());

                // this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
            }

            return existsResult;
        }

        /// <summary>
        /// Exists Post Function.
        /// This function supports analyzing the source for whether a given string exists in a given field within any known package.
        /// If any instance is found, the packageId in which the existence was found is returned for the caller to lookup more details
        /// as needed.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="durableClient">Durable client object.</param>
        /// <param name="logger">ILogger.</param>
        /// <param name="executionContext">Function execution context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName(FunctionConstants.ExistsPost)]
        public async Task<IActionResult> ExistsPostAsync(
            [HttpTrigger(AuthorizationLevel.Function, FunctionConstants.FunctionPost, Route = "Exists")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableClient,
            ILogger logger,
            ExecutionContext executionContext)
        {
            LoggingContext loggingContext = new LoggingContext();
            Dictionary<string, string> customDimensions = new Dictionary<string, string>();
            string orchestrationInstanceId;

            try
            {
                DiagnosticsHelper.Instance.SetupAzureFunctionLoggerAndGenevaTelemetry(
                    logger,
                    setupGenevaTelemetry: true,
                    monitorTenant: AzureFunctionEnvironment.MonitorTenant,
                    monitorRole: AzureFunctionEnvironment.MonitorRole);

                req.EnableBuffering();

                ContextTypeAndValueInputHelper requestData =
                    await RequestBodyHelper.GetRequestDataFromBody<ContextTypeAndValueInputHelper>(req.Body, true);
                loggingContext = DiagnosticsHelper.Instance.GetLoggingContext(
                    executionContext.FunctionName, executionContext.InvocationId.ToString(), requestData.OperationId);

                Logger.Info($"{loggingContext}Starting Exists processing. Received: {requestData}");
                customDimensions.Add("FunctionName", executionContext.FunctionName);
                customDimensions.Add("OperationId", requestData.OperationId);
                customDimensions.Add("Type", requestData.Type.ToString());
                customDimensions.Add("Value", requestData.Value);

                // this.telemetryClient.TrackEvent("ExecutionStart", customDimensions);
                ContextTypeAndValueInputHelper azureFunctionInputHelper =
                    new ContextTypeAndValueInputHelper(
                        requestData.OperationId,
                        requestData.Type,
                        requestData.Value);

                orchestrationInstanceId = await durableClient.StartNewAsync(
                    FunctionConstants.ExistsOrchestrator,
                    input: azureFunctionInputHelper);

                Logger.Info($"{loggingContext}{FunctionConstants.ExistsOrchestrator} " +
                    $"Orchestration instance id:  {orchestrationInstanceId}.");

                customDimensions.Add("Result", "Scheduled Exists operations.");
            }
            catch (Exception e)
            {
                Logger.Error($"{loggingContext}Error occurred : {e}");

                // this.telemetryClient.TrackException(e, customDimensions);
                customDimensions.Add("Exception", e.GetType().FullName);
                return new BadRequestObjectResult(new { Name = $"Error: {e}" });
            }
            finally
            {
                // this.telemetryClient.TrackEvent("ExecutionCompleted", customDimensions);
            }

            // This returns information that the client can use to query the status of the running orchestration.
            // We expect the client to poll for results and pull the success/fail of the operation from the output of the status response.
            // We are leveraging the full durable function pre-built infrastructure to offer our API Async.
            return durableClient.CreateCheckStatusResponse(req, orchestrationInstanceId);
        }
    }
}
