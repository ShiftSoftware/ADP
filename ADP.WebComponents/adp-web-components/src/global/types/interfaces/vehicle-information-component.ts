type VehicleInformationComponent = {
  isDev: boolean;
  baseUrl: string;
  queryString: string;
  /**
     
    */
};

//   @Prop() coreOnly: boolean = false;
//   @Prop() language: LanguageKeys = 'en';
//   @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
//   @Prop() loadingStateChange?: (isLoading: boolean) => void;
//   @Prop() loadedResponse?: (response: VehicleInformation) => void;

//   @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
//   @State() locale: InferType<typeof paintThicknessSchema> = paintThicknessSchema.getDefault();

//   @State() state: AppStates = 'idle';
//   @State() externalVin?: string = null;
//   @State() expandedImage?: string = null;
//   @State() errorMessage?: ErrorKeys = null;
//   @State() vehicleInformation?: VehicleInformation;

//   abortController: AbortController;
//   networkTimeoutRef: ReturnType<typeof setTimeout>;

//   originalImage: HTMLImageElement;

//   @Element() el: HTMLElement;

// =====================================

//   @Prop() language: LanguageKeys = 'en';
//   @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
//   @Prop() loadingStateChange?: (isLoading: boolean) => void;
//   @Prop() loadedResponse?: (response: PartInformation) => void;

//   @State() state: AppStates = 'idle';
//   @State() errorMessage?: ErrorKeys = null;
//   @State() partInformation?: PartInformation;
//   @State() externalPartNumber?: string = null;

//   @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
//   @State() locale: InferType<typeof deadStockSchema> = deadStockSchema.getDefault();

// ========================================
//   @Prop() headers: any = {};
//   @Prop() coreOnly: boolean = false;
//   @Prop() print?: (claimResponse: any) => void;
//   @Prop() maximumDocumentFileSizeInMb: number = 30;
//   @Prop() claimEndPoint: string = 'api/vehicle/swift-claim';
//   @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
//   @Prop() loadingStateChange?: (isLoading: boolean) => void;
//   @Prop() loadedResponse?: (response: VehicleInformation) => void;
//   @Prop() activate?: (vehicleInformation: VehicleInformation) => void;

//   @State() activeTab: string = '';
//   @State() isError: boolean = false;
//   @State() showPopup: boolean = false;
//   @State() isLoading: boolean = false;
//   @State() externalVin?: string = null;
//   @State() showPrintBox: boolean = false;
//   @State() errorMessage?: ErrorKeys = null;
//   @State() tabAnimationLoading: boolean = false;
//   @State() activePopupIndex: null | number = null;
//   @State() tabs: ServiceItemGroup[] = [];
//   @State() lastSuccessfulClaimResponse: any = null;
//   @State() vehicleInformation?: VehicleInformation;

//   pendingItemHighlighted = false;

//   @Element() el: HTMLElement;

//   scrollListenerRef: () => void;
//   abortController: AbortController;
//   timeoutRef: ReturnType<typeof setTimeout>;
//   networkTimeoutRef: ReturnType<typeof setTimeout>;
//   tabAnimationTimeoutRef: ReturnType<typeof setTimeout>;

//   cachedClaimItem: ServiceItem;

//   progressBar: HTMLElement;
//   popupPositionRef: HTMLElement;
//   claimForm: VehicleItemClaimForm;
//   claimableContentWrapper: HTMLElement;
export default VehicleInformationComponent;
