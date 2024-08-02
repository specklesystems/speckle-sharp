#include "PropertyExportManager.hpp"
#include "Utility.hpp"
#include "MD5Channel.hpp"

PropertyExportManager* PropertyExportManager::instance = nullptr;

PropertyExportManager* PropertyExportManager::GetInstance ()
{
	if (nullptr == instance) {
		instance = new PropertyExportManager;
	}
	return instance;
}


void PropertyExportManager::DeleteInstance ()
{
	if (nullptr != instance) {
		delete instance;
		instance = nullptr;
	}
}


PropertyExportManager::PropertyExportManager ()
{
	const GS::Guid	General3DLengthPropertyId = GS::Guid ("{BA0E29BD-A795-4A93-A33F-C2C17B46C33A}");
	const GS::Guid	General3DPerimeterPropertyId = GS::Guid ("{EEAD00FC-FBB0-4278-959D-C5244F66E8C1}");
	const GS::Guid	GeneralAbsoluteTopLinkStoryPropertyId = GS::Guid ("{7729820D-061B-4D5B-893B-8499E17D139D}");
	const GS::Guid	GeneralArchicadIFCIdPropertyId = GS::Guid ("{544CD642-5FDB-476D-B764-921825B4B681}");
	const GS::Guid	GeneralAreaPropertyId = GS::Guid ("{AC5CCA52-F79B-4850-92A9-BED7CB7C3847}");
	const GS::Guid	GeneralBottomElevationToFirstReferenceLevelPropertyId = GS::Guid ("{C17788DA-E680-46F7-B190-0E5BB26DA35F}");
	const GS::Guid	GeneralBottomElevationToHomeStoryPropertyId = GS::Guid ("{6CA8A0E2-340D-43D4-BA4B-5F3BF4CE660B}");
	const GS::Guid	GeneralBottomElevationToProjectZeroPropertyId = GS::Guid ("{6CE532E7-31EF-4623-B241-4DAC52BAC75F}");
	const GS::Guid	GeneralBottomElevationToSeaLevelPropertyId = GS::Guid ("{3617F61F-BAA9-48C4-AB5F-015A105D43A1}");
	const GS::Guid	GeneralBottomElevationToSecondReferenceLevelPropertyId = GS::Guid ("{F0A7B17E-A48F-4B51-8D26-9C6BC385DC90}");
	//const GS::Guid	GeneralBottomSurfacePropertyId = GS::Guid ("{B2237A3A-30B9-463F-B416-262A79BE3790}");
	//const GS::Guid	GeneralBuildingMaterialPropertyId = GS::Guid ("{B9179DB7-97BC-4985-94DC-06C201F79EF4}");
	//const GS::Guid	GeneralBuildingMaterialsPropertyId = GS::Guid ("{44C7C173-A14B-485E-9D64-DDF95465607F}");
	//const GS::Guid	GeneralBuildMatOrCompositOrProfileOrFillIndexPropertyId = GS::Guid ("{C9EDA02D-EA3A-4604-9E25-DD597361F472}");
	//const GS::Guid	GeneralCollidingZonesNamesPropertyId = GS::Guid ("{1CE1F282-DAA5-4F71-BEB9-E88022AA3369}");
	//const GS::Guid	GeneralComplexProfilePropertyId = GS::Guid ("{EAB6C4F7-A51F-4C13-BDBD-A72D4EFE9EC6}");
	//const GS::Guid	GeneralCompositeStructurePropertyId = GS::Guid ("{20CF3744-CA2D-4FB2-ACBC-F69C619AE362}");
	const GS::Guid	GeneralConditionalBottomSurfaceAreaPropertyId = GS::Guid ("{C332417B-48E4-4E89-8D39-14875A2C112F}");
	const GS::Guid	GeneralConditionalTopSurfaceAreaPropertyId = GS::Guid ("{5A60DDA4-5BFC-45EC-B8E6-D635E342550B}");
	//const GS::Guid	GeneralConditionalVolumePropertyId = GS::Guid ("{77786F55-28B4-4430-99B8-A00768AF86FA}");
	//const GS::Guid	GeneralCoverFillPropertyId = GS::Guid ("{811D313F-43F9-416D-BC29-5732A59C970A}");
	const GS::Guid	GeneralCrossSectionAreaAtBeginCutPropertyId = GS::Guid ("{094A5D1A-96EA-4485-8B7C-F1BAD5B17A26}");
	const GS::Guid	GeneralCrossSectionAreaAtEndCutPropertyId = GS::Guid ("{039E12FA-9A24-4D40-9B7E-5C54F26901CE}");
	const GS::Guid	GeneralCrossSectionHeightAtBeginCutPropertyId = GS::Guid ("{7A931D13-0C59-45A9-BBEF-A87EEB63C31B}");
	const GS::Guid	GeneralCrossSectionHeightAtBeginPerpendicularPropertyId = GS::Guid ("{830C1AC3-4B51-4F61-86F4-8CE0F8E4B828}");
	const GS::Guid	GeneralCrossSectionHeightAtEndCutPropertyId = GS::Guid ("{A7530CE2-3D5B-43A7-9768-1559ECAF9C84}");
	const GS::Guid	GeneralCrossSectionHeightAtEndPerpendicularPropertyId = GS::Guid ("{39C89AA7-32C5-494D-A9B7-8018E81439FE}");
	const GS::Guid	GeneralCrossSectionWidthAtBeginCutPropertyId = GS::Guid ("{44540399-FDC5-42CC-A250-9C8E58FF534E}");
	const GS::Guid	GeneralCrossSectionWidthAtBeginPerpendicularPropertyId = GS::Guid ("{FE71F960-6AEF-4B5B-9E65-08CA27E07AFF}");
	const GS::Guid	GeneralCrossSectionWidthAtEndCutPropertyId = GS::Guid ("{659A1AA0-72A0-465E-A45C-A619AFD2A912}");
	const GS::Guid	GeneralCrossSectionWidthAtEndPerpendicularPropertyId = GS::Guid ("{3DC2D3A5-E99C-4742-BEFF-43B34F721E82}");
	//const GS::Guid	GeneralEdgeSurfacePropertyId = GS::Guid ("{8781C839-32AB-4126-BA0B-F94757388438}");
	//const GS::Guid	GeneralElementClassificationPropertyId = GS::Guid ("{AB85B8B9-39EB-4730-8D3D-67226BA7D607}");
	const GS::Guid	GeneralElementIDPropertyId = GS::Guid ("{7E221F33-829B-4FBC-A670-E74DABCE6289}");
	const GS::Guid	GeneralElevationToFirstReferenceLevelPropertyId = GS::Guid ("{5794252D-2CF1-4D9F-863B-92EC259C2870}");
	const GS::Guid	GeneralElevationToProjectZeroPropertyId = GS::Guid ("{5BF6931F-82EB-40B8-B184-F51D20EC9D17}");
	const GS::Guid	GeneralElevationToSeaLevelPropertyId = GS::Guid ("{E4395866-E9A1-4D9D-81FD-A187464C1132}");
	const GS::Guid	GeneralElevationToSecondReferenceLevelPropertyId = GS::Guid ("{F2AF5F86-06AD-4E03-9C9B-0E7950E8B933}");
	const GS::Guid	GeneralElevationToStoryPropertyId = GS::Guid ("{1F477A91-E8B9-4F47-9EE6-18EDDB569BDA}");
	const GS::Guid	GeneralExternalIFCIdPropertyId = GS::Guid ("{E2C6EF45-1D1A-4D3A-99D2-E3A77A21DC75}");
	//const GS::Guid	GeneralFillTypePropertyId = GS::Guid ("{BE376478-6966-4892-BEC7-4CB72ABF4003}");
	//const GS::Guid	GeneralFloorPlanHolesPerimeterPropertyId = GS::Guid ("{05261268-6C0C-4FB7-A78B-15ECD5F7DF7E}");
	const GS::Guid	GeneralFloorPlanPerimeterPropertyId = GS::Guid ("{174704F5-98B7-429D-85C1-7E31A5AD9936}");
	//const GS::Guid	GeneralFromZoneNumberPropertyId = GS::Guid ("{1607B29C-1286-4EDF-AF50-A5CD6E555492}");
	//const GS::Guid	GeneralFromZonePropertyId = GS::Guid ("{EDC44824-1A6D-407A-9E39-693066622E7D}");
	const GS::Guid	GeneralGrossVolumePropertyId = GS::Guid ("{DB3A47B7-9723-47EB-B8BF-224761379150}");
	const GS::Guid	GeneralGrossBottomSurfaceAreaPropertyId = GS::Guid ("{FAB63421-E32C-40C0-9238-2ECE1BB1F499}");
	const GS::Guid	GeneralGrossEdgeSurfaceAreaPropertyId = GS::Guid ("{B06C9A3B-D304-45AB-A581-E6B42D104950}");
	const GS::Guid	GeneralGrossTopSurfaceAreaPropertyId = GS::Guid ("{3CFB8EEE-22BB-44FC-9B50-1C73ED16AC42}");
	const GS::Guid	GeneralHeightPropertyId = GS::Guid ("{C4B62357-1289-4D43-A3F6-AB02B192864C}");
	//const GS::Guid	GeneralHeightPropertyId_Obsolate = GS::Guid ("{121FD4D9-1116-4D36-9CFE-6972B377BA31}");
	//const GS::Guid	GeneralHoles3DPerimeterPropertyId = GS::Guid ("{8E19BE5F-D487-4BEC-9281-B34CA2355292}");
	const GS::Guid	GeneralHomeOffsetPropertyId = GS::Guid ("{FD43A58A-C7AC-4265-BB3B-CF5426D157C0}");
	//const GS::Guid	GeneralHomeStoryDisplayNumberPropertyId = GS::Guid ("{14D1CF3B-6A96-4E77-B84C-E39D407B1DEF}");
	//const GS::Guid	GeneralHomeStoryPropertyId = GS::Guid ("{8583BC95-C85D-48A3-9C2E-1EBED328BBFE}");
	//const GS::Guid	GeneralHotlinkAndElementIDPropertyId = GS::Guid ("{69A58F6F-DD3B-478D-B5EF-09A16BD0C548}");
	//const GS::Guid	GeneralHotlinkMasterIDPropertyId = GS::Guid ("{F98F297A-AFA7-4B0D-824B-975C78E02995}");
	const GS::Guid	GeneralInsulationSkinThicknessPropertyId = GS::Guid ("{E6927159-1AB9-47DB-9B77-4CCE95178D88}");
	//const GS::Guid	GeneralLastIssueDatePropertyId = GS::Guid ("{5F677AD9-ADCB-4662-83D2-0FE3E87F12D2}");
	//const GS::Guid	GeneralLastIssueIDPropertyId = GS::Guid ("{BF58FAAF-1BBE-43E3-B80A-694A9479EDD1}");
	//const GS::Guid	GeneralLastIssueNamePropertyId = GS::Guid ("{6EE454EB-2982-434C-B935-13190CD5989C}");
	//const GS::Guid	GeneralLayerIndexPropertyId = GS::Guid ("{25826253-26F2-4D30-BE1B-0FFBB1314903}");
	//const GS::Guid	GeneralLibraryPartNamePropertyId = GS::Guid ("{B8CFC590-58A3-43CF-A159-1B23BD3E8596}");
	//const GS::Guid	GeneralLinkedChangesPropertyId = GS::Guid ("{02956F1B-5058-40D1-A816-307074BA31E3}");
	//const GS::Guid	GeneralListingLabelTextPropertyId_Obsolete = GS::Guid ("{455180E3-DDD9-47D2-9E62-C1B5B513ABFD}");
	//const GS::Guid	GeneralLockedPropertyId = GS::Guid ("{419DA86C-86A9-4650-AD0E-965E8317D882}");
	const GS::Guid	GeneralNetBottomSurfaceAreaPropertyId = GS::Guid ("{F27621A7-CFDA-4674-8D14-4184EB3D210F}");
	const GS::Guid	GeneralNetEdgeSurfaceAreaPropertyId = GS::Guid ("{DEB28D76-C9B6-4D90-8260-857CD91FFA0D}");
	const GS::Guid	GeneralNetTopSurfaceAreaPropertyId = GS::Guid ("{A21B3448-7E50-4661-B57A-39841DD2FA1F}");
	const GS::Guid	GeneralNetVolumePropertyId = GS::Guid ("{FC8B1598-3E3B-4A4F-BBCE-277F83BC8598}");
	//const GS::Guid	GeneralOpeningIDsPropertyId = GS::Guid ("{ECEFBEB5-6901-40B0-AC4B-CC16ACCCC5F5}");
	//const GS::Guid	GeneralOpeningNumberPropertyId = GS::Guid ("{3E2F875B-5D53-4091-B302-D76DAD02AA70}");
	//const GS::Guid	GeneralOwnerIDPropertyId = GS::Guid ("{773AE220-E624-4FBA-8B8F-D56BB8FF6875}");
	//const GS::Guid	GeneralPropertyObjectNamePropertyId = GS::Guid ("{1EF4FF9C-CF55-4654-9821-F45B20BF6798}");
	//const GS::Guid	GeneralRelatedZoneNamePropertyId = GS::Guid ("{B466A457-B31F-4F1E-A91C-E255886AE93A}");
	//const GS::Guid	GeneralRelatedZoneNumberPropertyId = GS::Guid ("{140CAB27-7CCF-406F-B714-049FFB4B4D16}");
	const GS::Guid	GeneralRelativeTopLinkStoryPropertyId = GS::Guid ("{6D644AF5-B050-446F-883E-DB89AB3365A9}");
	const GS::Guid	GeneralSegmentCountPropertyId = GS::Guid ("{A2B5CD00-330B-4215-93B0-3CDB885F56DA}");
	const GS::Guid	GeneralSlantAnglePropertyId = GS::Guid ("{59D1CBE9-6C29-4B97-BA57-7EC9011B8D67}");
	const GS::Guid	GeneralStructureTypePropertyId = GS::Guid ("{7EC8476C-8E79-44D8-8BDE-F95C81F21EC2}");
	const GS::Guid	GeneralProfileCategoryPropertyId = GS::Guid ("{75A15148-1067-4F37-96E6-1F72DAFC749F}");
	const GS::Guid	GeneralSurfaceAreaPropertyId = GS::Guid ("{6AA4A58A-D32F-4AAB-BD84-E881F55D4122}");
	//const GS::Guid	GeneralSurfacesPropertyId = GS::Guid ("{1194ECB8-05C5-4EA2-9CC6-8096849840C8}");
	const GS::Guid	GeneralThicknessPropertyId = GS::Guid ("{A7B55E43-7C56-4C9E-836D-7A56F1D9D760}");
	const GS::Guid	GeneralTopElevationToFirstReferenceLevelPropertyId = GS::Guid ("{C215559A-308B-4722-A92D-E4CB5A352080}");
	const GS::Guid	GeneralTopElevationToHomeStoryPropertyId = GS::Guid ("{1ABB0A71-C54A-49BE-9281-C4D02AE5260C}");
	const GS::Guid	GeneralTopElevationToProjectZeroPropertyId = GS::Guid ("{4EB49FBE-218B-4938-B806-40D20A9E5E4D}");
	const GS::Guid	GeneralTopElevationToSeaLevelPropertyId = GS::Guid ("{1D554E08-F1D6-429D-B7B9-989310EB1BBF}");
	const GS::Guid	GeneralTopElevationToSecondReferenceLevelPropertyId = GS::Guid ("{ABA29B82-30C9-4E6C-87B6-B47F8E71A6CD}");
	const GS::Guid	GeneralTopLinkStoryPropertyId = GS::Guid ("{D01ECE7E-15FB-4B05-9B2F-112295E9DA48}");
	const GS::Guid	GeneralTopOffsetPropertyId = GS::Guid ("{1606C1A4-A80B-4EA6-A179-0983C8120B46}");
	//const GS::Guid	GeneralTopSurfacePropertyId = GS::Guid ("{21394C2E-9629-49A8-99B9-C7934DE3EFB0}");
	//const GS::Guid	GeneralToZoneNumberPropertyId = GS::Guid ("{746EEB4F-5DC1-44AC-B4DD-EA7958B6B227}");
	//const GS::Guid	GeneralToZonePropertyId = GS::Guid ("{679A16CA-D380-4945-9484-4F8510F93491}");
	const GS::Guid	GeneralTypePropertyId = GS::Guid ("{A16BA200-27C1-4D1A-9C7F-4F2F9C5C6E21}");
	const GS::Guid	GeneralUniqueIDPropertyId = GS::Guid ("{9C609FB7-E28E-4475-8ADC-E878E78A3858}");
	const GS::Guid	GeneralWidthPropertyId = GS::Guid ("{3799B10A-61C5-4566-BF9C-EAA9CE49196E}");

	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (General3DLengthPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (General3DPerimeterPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralAbsoluteTopLinkStoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralArchicadIFCIdPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralBottomElevationToFirstReferenceLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralBottomElevationToHomeStoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralBottomElevationToProjectZeroPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralBottomElevationToSeaLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralBottomElevationToSecondReferenceLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralConditionalBottomSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralConditionalTopSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionAreaAtBeginCutPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionAreaAtEndCutPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionHeightAtBeginCutPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionHeightAtBeginPerpendicularPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionHeightAtEndCutPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionHeightAtEndPerpendicularPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionWidthAtBeginCutPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionWidthAtBeginPerpendicularPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionWidthAtEndCutPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralCrossSectionWidthAtEndPerpendicularPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralElementIDPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralElevationToFirstReferenceLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralElevationToProjectZeroPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralElevationToSeaLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralElevationToSecondReferenceLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralElevationToStoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralExternalIFCIdPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralFloorPlanPerimeterPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralGrossVolumePropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralGrossBottomSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralGrossEdgeSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralGrossTopSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralHeightPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralHomeOffsetPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralInsulationSkinThicknessPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralNetBottomSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralNetEdgeSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralNetTopSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralNetVolumePropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralRelativeTopLinkStoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralSegmentCountPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralSlantAnglePropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralStructureTypePropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralProfileCategoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralSurfaceAreaPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralThicknessPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopElevationToFirstReferenceLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopElevationToHomeStoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopElevationToProjectZeroPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopElevationToSeaLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopElevationToSecondReferenceLevelPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopLinkStoryPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTopOffsetPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralTypePropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralUniqueIDPropertyId));
	propertyGroupFilter.elementPropertiesFilter.Add (GSGuid2APIGuid (GeneralWidthPropertyId));
	 
	propertyGroupFilter.componentPropertyGroupFilter.Add (APIGuidFromString ("BF31D3E0-A2B1-4543-A3DA-C1191D059FD8")); // Component_BuiltInPropertyGroupId
	propertyGroupFilter.componentPropertyGroupFilter.Add (APIGuidFromString ("3CF63E55-AA52-4AB4-B1C3-0920B2F352BF")); // BuildingMaterial_BuiltInPropertyGroupId

	complexElementsToSkipFromComponentListing.Add (API_CurtainWallID);
	complexElementsToSkipFromComponentListing.Add (API_StairID);
	complexElementsToSkipFromComponentListing.Add (API_RailingID);
}


GSErrCode FilterOutDefinitionsByDefinitions (GS::Array<API_PropertyDefinition>& definitions, const GS::Array<API_PropertyDefinition>& definitionsFilter)
{
	if (definitions.IsEmpty ())
		return NoError;

	if (definitionsFilter.IsEmpty ())
		return ErrParam;

	GS::Array<API_PropertyDefinition> filteredDefinitions;
	for (auto definition : definitions) {

		bool found = false;
		for (auto filterDefinition : definitionsFilter) {
			if (definition.guid == filterDefinition.guid) {
				found = true;
				break;
			}
		}

		if (!found)
			filteredDefinitions.Push (definition);
	}

	definitions = filteredDefinitions;

	return NoError;
}


GSErrCode FilterDefinitionsByDefinitionIds (GS::Array<API_PropertyDefinition>& definitions, const GS::HashSet<API_Guid>& definitionsFilter)
{
	if (definitions.IsEmpty ())
		return NoError;

	if (definitionsFilter.IsEmpty ()) {
		definitions.Clear ();
		return NoError;
	}

	GS::Array<API_PropertyDefinition> filteredDefinitions;
	for (auto definition : definitions) {
		if (definitionsFilter.Contains (definition.guid)) {
			filteredDefinitions.Push (definition);
		}
	}

	definitions = filteredDefinitions;

	return NoError;
}


GSErrCode FilterDefinitionsByPropertyGroup (GS::Array<API_PropertyDefinition>& definitions, const GS::HashSet<API_Guid>& propertyGroupsFilter)
{
	if (definitions.IsEmpty ())
		return NoError;

	if (propertyGroupsFilter.IsEmpty ()) {
		definitions.Clear ();
		return NoError;
	}

	GS::Array<API_PropertyDefinition> filteredDefinitions;
	for (auto definition : definitions) {
		if (propertyGroupsFilter.Contains (definition.groupGuid)) {
			filteredDefinitions.Push (definition);
		}
	}

	definitions = filteredDefinitions;

	return NoError;
}


GS::UInt64 GenerateFingerPrint (const API_ElemType& elementType, const bool& sendProperties, const bool& sendListingParameters, const GS::Array<GS::Pair<API_Guid, API_Guid>>& systemItemPairs)
{
	IO::MD5Channel md5Channel;
	MD5::FingerPrint checkSum;

	md5Channel.Write (elementType.typeID);
	md5Channel.Write (elementType.variationID);
	md5Channel.Write (sendProperties);
	md5Channel.Write (sendListingParameters);

	for (auto id : systemItemPairs) {
		md5Channel.Write (APIGuid2GSGuid (id.first));
		md5Channel.Write (APIGuid2GSGuid (id.second));
	}

	md5Channel.Finish (&checkSum);
	return checkSum.GetUInt64Value ();
}


/*!
 Get property definitions for a specified element
 @param element The target element to retrieve the property definitions for
 @param sendProperties True to export the Archicad properties attached to the element
 @param sendListingParameters True to export calculated listing parameters from the element, e.g. top/bottom surface area etc
 @param systemItemPairs Array pairing a classification system ID against a classification item ID (attached to the target element)
 @param elementDefinitions The element property definitions (retrieved in this function)
 @param componentsDefinitions The component property definitions (paired with the component ID, retrieved in this function)
 @return NoError if the definitions were retrieved without errors
 */
GSErrCode PropertyExportManager::GetElementDefinitions (const API_Element& element, const bool& sendProperties, const bool& sendListingParameters, const GS::Array<GS::Pair<API_Guid, API_Guid>>& systemItemPairs, GS::Array<API_PropertyDefinition>& elementDefinitions, GS::Array<GS::Pair<API_ElemComponentID, GS::Array<API_PropertyDefinition>>>& componentsDefinitions)
{
	GSErrCode err = NoError;

	API_ElemType elementType = Utility::GetElementType (element.header);

	GS::Array<API_PropertyDefinition> elementUserDefinedDefinitions;
	
	// element-level properties
	{
			//Create a hash value for target element and prefs
		GS::UInt64 fingerPrint = GenerateFingerPrint (elementType, sendProperties, sendListingParameters, systemItemPairs);
			//If we've already encountered this combo, use the property definitions we already found (will always be the same - saves a lot of time)
		if (cache.ContainsKey (fingerPrint)) {
			elementDefinitions = cache.Get (fingerPrint).first;
			elementUserDefinedDefinitions = cache.Get (fingerPrint).second;
		} else {
			if (sendProperties) {
					//Collect user-defined property definitions for the target element when the user requests them
				err = ACAPI_Element_GetPropertyDefinitions (element.header.guid, API_PropertyDefinitionFilter_UserDefined, elementUserDefinedDefinitions);
				if (err != NoError)
					return err;
			}

			GS::Array<API_PropertyDefinition> elementUserLevelBuiltInDefinitions;
			if (sendListingParameters) {
					//Collect built-in property definitions for the target element when the user requests them
				err = ACAPI_Element_GetPropertyDefinitions (element.header.guid, API_PropertyDefinitionFilter_UserLevelBuiltIn, elementUserLevelBuiltInDefinitions);
				if (err != NoError)
					return err;
					//The list of definitions can include many things we don't want - filter it to definitions we're really interested in
				err = FilterDefinitionsByDefinitionIds (elementUserLevelBuiltInDefinitions, propertyGroupFilter.elementPropertiesFilter);
				if (err != NoError)
					return err;
			}

			elementDefinitions = elementUserDefinedDefinitions;
			elementDefinitions.Append (elementUserLevelBuiltInDefinitions);
				//Add the definitions to the cache to save looking them up again for the same target specs
			cache.Add (fingerPrint, GS::Pair<GS::Array<API_PropertyDefinition>, GS::Array<API_PropertyDefinition>> (elementDefinitions, elementUserDefinedDefinitions));
		}
	}

	// components properties
	// because of performance reasons components are skipped for complex elements
	if (complexElementsToSkipFromComponentListing.Contains (elementType))
		return NoError;

	GS::Array<API_ElemComponentID> components;
	err = ACAPI_Element_GetComponents (element.header.guid, components);
	if (err != NoError)
		return err;

	componentsDefinitions.Clear ();
	for (auto& component : components) {
		GS::Array<API_PropertyDefinition> componentUserDefinedDefinitions;

		if (sendProperties) {
#ifdef ServerMainVers_2700
			err = ACAPI_Element_GetPropertyDefinitions (component, API_PropertyDefinitionFilter_UserDefined, componentUserDefinedDefinitions);
#else
			err = ACAPI_ElemComponent_GetPropertyDefinitions (component, API_PropertyDefinitionFilter_UserDefined, componentUserDefinedDefinitions);
#endif
			if (err != NoError)
				continue;

			err = FilterOutDefinitionsByDefinitions (componentUserDefinedDefinitions, elementUserDefinedDefinitions);
			if (err != NoError)
				continue;
		}

		GS::Array<API_PropertyDefinition> componentUserLevelBuiltInDefinitions;
		if (sendListingParameters) {
#ifdef ServerMainVers_2700
			err = ACAPI_Element_GetPropertyDefinitions (component, API_PropertyDefinitionFilter_UserLevelBuiltIn, componentUserLevelBuiltInDefinitions);
#else
			err = ACAPI_ElemComponent_GetPropertyDefinitions (component, API_PropertyDefinitionFilter_UserLevelBuiltIn, componentUserLevelBuiltInDefinitions);
#endif
			if (err != NoError)
				continue;

			err = FilterDefinitionsByPropertyGroup (componentUserLevelBuiltInDefinitions, propertyGroupFilter.componentPropertyGroupFilter);
			if (err != NoError)
				continue;
		}

		componentUserDefinedDefinitions.Append (componentUserLevelBuiltInDefinitions);

		componentsDefinitions.Push (GS::Pair<API_ElemComponentID, GS::Array<API_PropertyDefinition>> (component, componentUserDefinedDefinitions));
	}

	return NoError;
}
