using Speckle.Core.Api;

namespace TestsIntegration;

public class AutomationContextTests
{

  private async void _registerNewAutomation(string projectId, string modelId, Client speckleClient, string automationId, string automationName, string automationRevisionId)
  {

  }
}
/*
 
def crypto_random_string(length: int) -> str:
    """Generate a semi crypto random string of a given length."""
    alphabet = string.ascii_letters + string.digits
    return "".join(secrets.choice(alphabet) for _ in range(length))


def register_new_automation(
    project_id: str,
    model_id: str,
    speckle_client: SpeckleClient,
    automation_id: str,
    automation_name: str,
    automation_revision_id: str,
):
    """Register a new automation in the speckle server."""
    query = gql(
        """
        mutation CreateAutomation(
            $projectId: String! 
            $modelId: String! 
            $automationName: String!
            $automationId: String! 
            $automationRevisionId: String!
        ) {
                automationMutations {
                    create(
                        input: {
                            projectId: $projectId
                            modelId: $modelId
                            automationName: $automationName 
                            automationId: $automationId
                            automationRevisionId: $automationRevisionId
                        }
                    )
                }
            }
        """
    )
    params = {
        "projectId": project_id,
        "modelId": model_id,
        "automationName": automation_name,
        "automationId": automation_id,
        "automationRevisionId": automation_revision_id,
    }
    speckle_client.httpclient.execute(query, params)


@pytest.fixture()
def speckle_token(user_dict: Dict[str, str]) -> str:
    """Provide a speckle token for the test suite."""
    return user_dict["token"]


@pytest.fixture()
def speckle_server_url(host: str) -> str:
    """Provide a speckle server url for the test suite, default to localhost."""
    return f"http://{host}"


@pytest.fixture()
def test_client(speckle_server_url: str, speckle_token: str) -> SpeckleClient:
    """Initialize a SpeckleClient for testing."""
    test_client = SpeckleClient(speckle_server_url, use_ssl=False)
    test_client.authenticate_with_token(speckle_token)
    return test_client


@pytest.fixture()
def test_object() -> Base:
    """Create a Base model for testing."""
    root_object = Base()
    root_object.foo = "bar"
    return root_object


@pytest.fixture()
def automation_run_data(
    test_object: Base, test_client: SpeckleClient, speckle_server_url: str
) -> AutomationRunData:
    """Set up an automation context for testing."""
    project_id = test_client.stream.create("Automate function e2e test")
    branch_name = "main"

    model = test_client.branch.get(project_id, branch_name, commits_limit=1)
    model_id: str = model.id

    root_obj_id = operations.send(
        test_object, [ServerTransport(project_id, test_client)]
    )
    version_id = test_client.commit.create(project_id, root_obj_id)

    automation_name = crypto_random_string(10)
    automation_id = crypto_random_string(10)
    automation_revision_id = crypto_random_string(10)

    register_new_automation(
        project_id,
        model_id,
        test_client,
        automation_id,
        automation_name,
        automation_revision_id,
    )

    automation_run_id = crypto_random_string(10)
    function_id = crypto_random_string(10)
    function_release = crypto_random_string(10)
    return AutomationRunData(
        project_id=project_id,
        model_id=model_id,
        branch_name=branch_name,
        version_id=version_id,
        speckle_server_url=speckle_server_url,
        automation_id=automation_id,
        automation_revision_id=automation_revision_id,
        automation_run_id=automation_run_id,
        function_id=function_id,
        function_release=function_release,
    )


def get_automation_status(
    project_id: str,
    model_id: str,
    speckle_client: SpeckleClient,
):
    query = gql(
        """
query AutomationRuns(
            $projectId: String! 
            $modelId: String! 
    )
{
  project(id: $projectId) {
    model(id: $modelId) {
      automationStatus {
        id
        status
        statusMessage
        automationRuns {
          id
          automationId
          versionId
          createdAt
          updatedAt
          status
          functionRuns {
            id
            functionId
            elapsed
            status
            contextView
            statusMessage
            results
            resultVersions {
              id
            }
          }
        }
      }
    }
  }
}
        """
    )
    params = {
        "projectId": project_id,
        "modelId": model_id,
    }
    response = speckle_client.httpclient.execute(query, params)
    return response["project"]["model"]["automationStatus"]


class FunctionInputs(AutomateBase):
    forbidden_speckle_type: str


def automate_function(
    automate_context: AutomationContext,
    function_inputs: FunctionInputs,
) -> None:
    """Hey, trying the automate sdk experience here."""
    version_root_object = automate_context.receive_version()

    count = 0
    if version_root_object.speckle_type == function_inputs.forbidden_speckle_type:
        if not version_root_object.id:
            raise ValueError("Cannot operate on objects without their id's.")
        automate_context.add_object_error(
            version_root_object.id,
            "This project should not contain the type: "
            f"{function_inputs.forbidden_speckle_type}",
        )
        count += 1

    if count > 0:
        automate_context.mark_run_failed(
            "Automation failed: "
            f"Found {count} object that have a forbidden speckle type: "
            f"{function_inputs.forbidden_speckle_type}"
        )

    else:
        automate_context.mark_run_success("No forbidden types found.")


def test_function_run(automation_run_data: AutomationRunData, speckle_token: str):
    """Run an integration test for the automate function."""
    automation_context = run_function(
        automate_function,
        automation_run_data,
        speckle_token,
        FunctionInputs(forbidden_speckle_type="Base"),
    )

    assert automation_context.run_status == AutomationStatus.FAILED
    status = get_automation_status(
        automation_run_data.project_id,
        automation_run_data.model_id,
        automation_context.speckle_client,
    )
    assert status["status"] == automation_context.run_status
    status_message = status["automationRuns"][0]["functionRuns"][0]["statusMessage"]
    assert status_message == automation_context._automation_result.status_message


def test_file_uploads(automation_run_data: AutomationRunData, speckle_token: str):
    """Test file store capabilities of the automate sdk."""
    automation_context = AutomationContext.initialize(
        automation_run_data, speckle_token
    )

    path = Path(f"./{crypto_random_string(10)}").resolve()
    path.write_text("foobar")

    automation_context.store_file_result(path)

    os.remove(path)
    assert len(automation_context._automation_result.blobs) == 1
 */
