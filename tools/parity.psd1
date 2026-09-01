#
# Per-group configuration for the endpoint-parity harness.
#
# TEMPORARY - deleted in Step 08 with the rest of the harness.
# See docs/planning/shift-framework-upgrade/verification.md.
#
# EVERY setting in this file WIDENS what the harness tolerates. That makes every entry a
# deliberate, reviewed act rather than a default:
#
#   orderInsensitive   - stops a collection's ORDER from being compared (Rule 4). Sort order is
#                        semantic in several places, so an entry here trades away an ordering
#                        regression. Add one only when the order is genuinely unspecified.
#   excludedRoutes     - stops a catalogue route from needing a case (section 5). Every entry
#                        needs a written reason; without this list an uncovered route is simply
#                        invisible.
#   writeUnreachable   - exempts an entity from the 100% CREATE/UPDATE 2xx gate. Every entry
#                        needs a mapper-level write golden INSTEAD, because a triple with
#                        neither is a write mapper nothing tests.
#   restrictedGrant    - what "Restricted" means for this group. Each group has its own action
#                        tree, so this has no group-independent meaning and MUST be declared
#                        per group.
#
# The numbers in restrictedGrant are the TypeAuth Access enum: Read=1, Write=2, Delete=3,
# Maximum=4. Full access is [1,2,3,4] on every tree, which is what SetFullAccessAsync produces.
#

@{
    # ---------------------------------------------------------------------------------------
    # Shared defaults. A group's own block overrides these.
    # ---------------------------------------------------------------------------------------
    Defaults = @{
        # Rule 3: only these response headers are captured, on top of status and Content-Type.
        # Everything else is dropped as volatile-by-construction.
        headerAllowlist = @('Content-Language')

        # Rule 6: en-US on every request, plus one extra pass at a second culture, because
        # number and date formatting differences would otherwise hide inside a single culture.
        cultures        = @('en-US', 'ar-IQ')

        # Rule 4: no collection is order-insensitive by default. This is the strict case.
        orderInsensitive = @()

        # Pinned so that same salt + same seeded long = same hash id (Rule 1).
        hashIdSalt      = 's-u-r-v-e-y-s-s-a-l-t-v1'
        hashIdMinLength = 5
    }

    Groups = @{

        # -----------------------------------------------------------------------------------
        Menus = @{
            hostMode        = 'Sample'          # ADP.Menus.Sample.API
            routePrefix     = 'api/Menus'
            actionTrees     = @('ShiftIdentityActions', 'AzureStorageActionTree', 'GeneralActionTree', 'MenusActionTree')
            connectionKey   = 'ConnectionStrings:SQLServer'

            # The Menus sample configures Cosmos; emptying it skips the whole replication +
            # provisioning block and removes replication side effects from the write cases.
            configOverrides = @{
                'ConnectionStrings:Cosmos' = ''
            }

            restrictedGrant = @{ MenusActionTree = @(1) }   # read-only

            excludedRoutes  = @()
            writeUnreachable = @()
            orderInsensitive = @()
        }

        # -----------------------------------------------------------------------------------
        Darlastic = @{
            hostMode        = 'Sample'          # ADP.Darlastic.Sample.API
            routePrefix     = 'api/Darlastic'
            actionTrees     = @('DarlasticActionTree')
            connectionKey   = 'ConnectionStrings:Registry'
            configOverrides = @{
                'Sample:AllowDevAuth' = 'true'
            }

            # 0 triples, 0 profiles. The plan's ONLY framework-only control: a diff here is
            # unambiguously framework-caused, which is what every mapper group's diff gets
            # attributed against. Blocked on SPIKE-5 (the host return 1s on missing config and
            # needs a populated registry the repo does not seed).
            restrictedGrant = @{ DarlasticActionTree = @(1) }

            excludedRoutes  = @()
            writeUnreachable = @()
            orderInsensitive = @()
        }

        # -----------------------------------------------------------------------------------
        Surveys = @{
            hostMode        = 'Sample'          # ADP.Surveys.Sample.API
            routePrefix     = 'api/Surveys'
            actionTrees     = @('ShiftIdentityActions', 'AzureStorageActionTree', 'GeneralActionTree', 'SurveysActionTree')
            connectionKey   = 'ConnectionStrings:SQLServer'
            configOverrides = @{}

            restrictedGrant = @{ SurveysActionTree = @(1) }   # read-only

            # SurveyInstanceController overrides GetSingle/Post/Put/Delete/GetRevisions/Print/
            # PrintToken to return 405, yet SurveyInstanceRepository is a real triple whose
            # MapToEntityGenerated is live - driven from the public submit and trigger-ingest
            # paths. Left alone the harness produces 405 transcripts, passes "0 5xx", and covers
            # that write mapper NOT AT ALL. It therefore needs a mapper-level write golden.
            writeUnreachable = @(
                @{ entity = 'SurveyInstance'
                   reason = 'Controller overrides all write verbs to 405; the triple is driven from the public submit and trigger-ingest paths instead. Substitute in place: ADP.Surveys/ADP.Surveys.Data.Tests/SurveyInstanceWriteMapperGoldenTests.cs, which diffs every scalar property across MapToEntity and asserts the written set is exactly the five audit members - no domain member. It lives outside ADP.EndpointParity deliberately, so it survives the harness deletion; the 405s are permanent, so the substitute has to be too.' }
            )

            excludedRoutes  = @()
            orderInsensitive = @()
        }

        # -----------------------------------------------------------------------------------
        ClaimableItems = @{
            hostMode        = 'Mounted'         # no sample host exists - SPIKE-2 proved this boots
            routePrefix     = 'api/ClaimableItems'
            actionTrees     = @('ClaimableItemsActionTree')
            connectionKey   = $null             # mounted host builds its own connection string
            configOverrides = @{}

            restrictedGrant = @{ ClaimableItemsActionTree = @(1) }

            excludedRoutes  = @()
            writeUnreachable = @()
            orderInsensitive = @()
        }

        # -----------------------------------------------------------------------------------
        WarrantyClaims = @{
            hostMode        = 'Mounted'         # no sample host exists
            routePrefix     = 'api/WarrantyClaims'
            actionTrees     = @('WarrantyClaimsActionTree')
            connectionKey   = $null
            configOverrides = @{}

            restrictedGrant = @{ WarrantyClaimsActionTree = @(1) }

            # The dealer/distributor exposure IS visible on the ordinary full-access pass:
            # DealerFinancialController is a separate route with its own DTO, not a
            # privilege-filtered projection, and DealerFinancialRepository applies no row
            # scoping. The restricted pass is still mandatory as an independent control.
            excludedRoutes  = @()
            writeUnreachable = @()
            orderInsensitive = @()
        }
    }
}
