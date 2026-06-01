import { Component, Prop, State, Watch, h, Host, Method, Element } from '@stencil/core';
import { DotNetObjectReference } from '~features/blazor-ref';
import cn from '~lib/cn';
import validateVin from '~lib/validate-vin';

const CAPTURE_INTERVAL = 2000;
const FACING_MODE_KEY = 'cameraFacingMode';

type CameraFacing = 'environment' | 'user';

@Component({
  shadow: true,
  tag: 'vin-extractor',
  styleUrl: 'vin-extractor.css',
})
export class VinExtractor {
  @Prop() title: string = '';
  @Prop() captureInterval: number = CAPTURE_INTERVAL;

  @Prop() verbose: boolean = false;

  @Prop() useOcr: boolean = false;
  @Prop() readSticker: boolean = false;

  @Prop() uploaderButtonId: string;

  // to explicitly opening camera instead of letting choose from files or new image from camera
  @Prop() captureEnvironment: boolean = false;

  @Prop() manualCapture: boolean = false;
  @Prop() skipValidation: boolean = false;

  @Prop() manualCaptureLabel: string = '';

  @Prop() manualCaptureHint: string = '';

  @Prop() ocrEndpoint: string;

  @Prop() onExtract?: ((vin: string) => void) | string;
  @Prop() onError?: ((newError: Error) => void) | string;
  @Prop() onProcessing?: ((vin: string) => void) | string;
  @Prop() onOpenChange?: ((newError: boolean) => void) | string;

  @State() streamRef: MediaStream;
  @State() isOpen: boolean = false;
  @State() isAnimating: boolean = false;
  @State() isCameraReady: boolean = false;
  @State() switchRotateDegree: number = 0;
  @State() containerAnimation: string = '';
  @State() blazorRef?: DotNetObjectReference;
  @State() videoInputs: MediaDeviceInfo[] = [];
  @State() manualCaptureLoading: boolean = false;
  @State() facingMode: CameraFacing = localStorage.getItem(FACING_MODE_KEY) === 'user' ? 'user' : 'environment';

  @Element() el: HTMLElement;

  private codeReader: any;
  private fileInput: HTMLInputElement;
  private videoPlayer: HTMLVideoElement;
  private fileButton: HTMLButtonElement;
  private videoCanvas: HTMLCanvasElement;
  private abortController: AbortController;
  private frameCaptureTimeoutRef: ReturnType<typeof setTimeout>;
  private backCameraDeviceId: string = '';

  async componentDidLoad() {
    this.videoPlayer = this.el.shadowRoot.querySelector('.video-player');
    this.videoCanvas = this.el.shadowRoot.querySelector('.video-canvas');
    this.abortController = new AbortController();

    if (!this.readSticker && this.uploaderButtonId) this.registerFileUploader();

    if (this.readSticker) {
      const ZXingSrc = 'https://unpkg.com/@zxing/library@0.21.3/umd/index.min.js';

      const alreadyLoaded = Array.from(document.scripts).some(script => script.src === ZXingSrc);

      if (alreadyLoaded && this.uploaderButtonId) this.registerFileUploader();

      if (!alreadyLoaded) {
        const script = document.createElement('script');
        script.src = ZXingSrc;
        script.defer = true;
        document.head.appendChild(script);
        script.onload = () => {
          // @ts-ignore
          if (ZXing) this.codeReader = new ZXing.BrowserMultiFormatReader();
        };
      } else if (!this.codeReader) {
        try {
          // @ts-ignore
          if (ZXing) this.codeReader = new ZXing.BrowserMultiFormatReader();
        } catch (error) {
          setTimeout(() => {
            this.componentDidLoad();
          }, 100);
        }
      }
    }
  }

  registerFileUploader = () => {
    if (this.readSticker && !this.codeReader) {
      setTimeout(() => {
        this.componentDidLoad();
      }, 100);
      return;
    }

    this.fileButton = document.querySelector('#' + this.uploaderButtonId);
    this.fileInput = this.el.shadowRoot.querySelector('.vin-extractor-input');

    this.fileButton.removeEventListener('click', this.onFileUploaderClick);
    this.fileInput.removeEventListener('change', this.onFileUploaderChange);

    this.fileButton.addEventListener('click', this.onFileUploaderClick);
    this.fileInput.addEventListener('change', this.onFileUploaderChange);
  };

  onFileUploaderClick = () => {
    this.fileInput.click();
  };

  onFileUploaderChange = () => {
    const file = this.fileInput.files?.[0];
    if (!file) return;

    const img = new Image();
    const reader = new FileReader();

    reader.onload = () => {
      img.src = reader.result as string;
    };

    img.onload = async () => {
      this.handleOnProcessing(true);

      const imageDataUrl = await this.processCanvasFromSource(img, this.videoCanvas);
      await this.handleImage(imageDataUrl);

      this.handleOnProcessing(false);
    };

    reader.readAsDataURL(file);
  };

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }

  triggerCallback = (callback: any, ...args: any[]) => {
    if (callback) {
      if (typeof callback === 'function') callback(...args);
      else if (this.blazorRef && typeof callback === 'string' && !!callback) this.blazorRef?.invokeMethodAsync(callback, ...args);
    }
  };

  handleError = (error: any) => {
    this.triggerCallback(this.onError, error as Error);
  };

  handleExtract = (vin: string) => {
    this.triggerCallback(this.onExtract, vin);
  };

  handleOnProcessing = (isProcessing: boolean) => {
    this.triggerCallback(this.onProcessing, isProcessing);
  };

  captureFrame = async (manualCapture: boolean = false) => {
    if (!this.isOpen) return;

    if (!this.videoPlayer || !this.videoCanvas) return this.componentDidLoad();

    this.handleOnProcessing(true);

    if (manualCapture) {
      this.videoPlayer.pause();
      this.manualCaptureLoading = true;
    }

    const imageDataUrl = await this.processCanvasFromSource(this.videoPlayer, this.videoCanvas);

    this.handleImage(imageDataUrl);

    if (!this.manualCapture) {
      this.frameCaptureTimeoutRef = setTimeout(this.captureFrame, this.captureInterval);
      this.handleOnProcessing(false);
    }

    if (manualCapture) {
      setTimeout(() => {
        this.videoPlayer.play();
        this.manualCaptureLoading = false;
        this.handleOnProcessing(false);
        // Re-engage autofocus last: some devices drop to idle focus after the
        // pause. Fire-and-forget so it can't affect the resume flow.
        this.applyAutofocus();
      }, 1000);
    }
  };

  async processCanvasFromSource(source: HTMLImageElement | HTMLVideoElement, canvas: HTMLCanvasElement, maxSize: number = 400): Promise<string> {
    try {
      const isImage = source instanceof HTMLImageElement;

      const naturalWidth = isImage ? source.width : source.videoWidth;
      const naturalHeight = isImage ? source.height : source.videoHeight;

      let targetWidth = naturalWidth;
      let targetHeight = naturalHeight;

      if (Math.max(naturalWidth, naturalHeight) > maxSize) {
        const scale = maxSize / Math.max(naturalWidth, naturalHeight);
        targetWidth = Math.round(naturalWidth * scale);
        targetHeight = Math.round(naturalHeight * scale);
      }

      canvas.width = targetWidth;
      canvas.height = targetHeight;

      const ctx = canvas.getContext('2d');

      ctx.drawImage(source, 0, 0, targetWidth, targetHeight);

      return canvas.toDataURL('image/png');
    } catch (error) {
      this.handleError(error as Error);
      return '';
    }
  }

  handleImage = async (imageDataUrl: string) => {
    if (this.readSticker) await this.stickerHandler(imageDataUrl);

    if (this.useOcr && this.ocrEndpoint) await this.ocrHandler(imageDataUrl);
  };

  ocrHandler = async (imageDataUrl: string) => {
    try {
      const response = await fetch(this.ocrEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        signal: this.abortController.signal,
        body: JSON.stringify({ image: imageDataUrl.split(',')[1] }),
      });

      if (!response.ok) throw new Error('Failed to fetch OCR result');

      if (this.verbose) console.log(response);

      const data: string = await response.text();

      if (this.skipValidation || (!!data.trim() && validateVin(data))) this.handleExtract(data);
    } catch (error) {
      this.handleError(error as Error);
    }
  };

  stickerHandler = async (imageDataUrl: string) => {
    try {
      if (this.verbose) console.log('try detecting sticker');

      const result = await this.codeReader.decodeFromImage(undefined, imageDataUrl);
      const text = result.getText();
      if (this.verbose) console.log(text);

      if (!!text.trim()) {
        if (this.skipValidation) this.handleExtract(text.trim());
        else {
          const vin = text.replace(/[qo]/g, '0').replace(/i/g, '1').replace(/ /g, '');

          if (vin.length === 17 && validateVin(vin)) this.handleExtract(vin);
        }
      }
    } catch (error) {
      this.handleError(error as Error);
    }
  };

  openScanner = async () => {
    try {
      this.abortController?.abort();
      this.abortController = new AbortController();

      const permissionStatus = await navigator.permissions.query({
        // @ts-ignore
        name: 'camera',
      });

      if (permissionStatus.state === 'prompt') {
        try {
          const probe = await navigator.mediaDevices.getUserMedia({
            video: true,
          });
          probe.getTracks().forEach(track => track.stop());
        } catch (error) {
          throw new Error('no camera access');
        }
      }

      if (permissionStatus.state === 'denied') {
        throw new Error('no camera access');
      }

      // We only need the count here — to decide whether to show the front/back
      // toggle. The lens choice itself is delegated to facingMode in startCamera,
      // so we no longer match against device labels (empty/non-standard on Android).
      const devices = await navigator.mediaDevices.enumerateDevices();
      const videoInputs = devices.filter(device => device.kind === 'videoinput');

      if (videoInputs.length === 0) throw new Error('No Camera Found');

      this.videoInputs = videoInputs;

      await this.startCamera();

      if (!this.manualCapture) this.frameCaptureTimeoutRef = setTimeout(this.captureFrame, this.captureInterval + 300);

      if (document) document.body.style.overflow = 'hidden';

      this.isCameraReady = true;

      this.containerAnimation = 'show-container';
    } catch (error) {
      this.handleError(error);
    }
  };

  @Method()
  async open() {
    this.isOpen = true;
  }

  @Method()
  async close() {
    this.isOpen = false;
  }

  // Every acquisition carries an advisory continuous-autofocus hint. It's
  // best-effort: devices that don't support focusMode (e.g. iOS, which focuses
  // natively) ignore it, so it never throws or over-constrains. See applyAutofocus
  // for the post-stream re-assert.
  private withAutofocus = (video: MediaTrackConstraints): MediaTrackConstraints => ({ ...video, advanced: [{ focusMode: 'continuous' } as any] });

  // Open a camera by direction. `exact` forces the side; the fallback drops to an
  // advisory facingMode so a device lacking that side (e.g. a front-only webcam)
  // doesn't hard-fail with OverconstrainedError.
  private openFacing = (facing: CameraFacing): Promise<MediaStream> =>
    navigator.mediaDevices
      .getUserMedia({ video: this.withAutofocus({ facingMode: { exact: facing } }) })
      .catch(() => navigator.mediaDevices.getUserMedia({ video: this.withAutofocus({ facingMode: facing }) }));

  private openDeviceId = (deviceId: string): Promise<MediaStream> => navigator.mediaDevices.getUserMedia({ video: this.withAutofocus({ deviceId: { exact: deviceId } }) });

  getCameraStream = async (facing: CameraFacing): Promise<MediaStream> => {
    try {
      return facing === 'user' ? await this.openFacing('user') : await this.openBackCamera();
    } catch (error) {
      // Any failure in smart selection falls back to the plain default for that
      // side — i.e. exactly the pre-selection behavior. Never leaves us with no camera.
      if (this.verbose) console.warn('vin-extractor camera selection failed, using default', error);
      return this.openFacing(facing);
    }
  };

  // facingMode picks *a* back camera, but when several rear lenses share
  // facingMode:'environment' (Galaxy A25 = main + ultrawide + macro) the browser
  // returns an implementation-defined default — often a fixed-focus ultrawide/macro
  // that can never focus on a close QR/VIN. So: take the default, and only if it
  // can't autofocus do we probe the other lenses and pick the best one. Most
  // devices (incl. iOS) keep the default with no extra camera opens.
  private openBackCamera = async (): Promise<MediaStream> => {
    // Reuse the lens we picked earlier this session (kept in memory only — no
    // localStorage, so there's no stale pick to carry across sessions or devices).
    if (this.backCameraDeviceId) {
      try {
        return await this.openDeviceId(this.backCameraDeviceId);
      } catch (_) {
        this.backCameraDeviceId = ''; // lens vanished — re-pick
      }
    }

    const def = await this.openFacing('environment');
    const track = def.getVideoTracks()[0];
    const caps: any = track?.getCapabilities?.();

    if (this.verbose) console.log('vin-extractor default back lens', track?.getSettings?.()?.deviceId, caps);

    // Probe only when the default can't autofocus AND the platform exposes focus
    // control (focusMode array) or is Android. iOS doesn't expose focusMode and
    // focuses natively, so it always keeps the default — no regression there.
    const isAndroid = /android/i.test(navigator.userAgent);
    const needsBetterLens = !!caps && !this.lensAutofocuses(caps) && (Array.isArray(caps.focusMode) || isAndroid);

    if (!needsBetterLens) return def;

    const defId: string | undefined = track?.getSettings?.().deviceId;
    def.getTracks().forEach(t => t.stop());

    const bestId = await this.pickBackCameraDeviceId(defId);
    if (bestId) {
      try {
        const best = await this.openDeviceId(bestId);
        this.backCameraDeviceId = bestId; // remember for this session
        if (this.verbose) console.log('vin-extractor selected back lens', bestId);
        return best;
      } catch (_) {
        /* fall through to default */
      }
    }

    // Nothing better available/openable — fall back to the OS default back camera.
    return this.openFacing('environment');
  };

  // Open each non-front lens briefly (one at a time — some devices forbid
  // concurrent camera use), read capabilities, and return the highest-scoring
  // deviceId. Labels are only a cheap pre-filter to avoid waking the front camera;
  // the facingMode capability is authoritative.
  private pickBackCameraDeviceId = async (excludeDeviceId?: string): Promise<string | null> => {
    let devices: MediaDeviceInfo[];
    try {
      devices = await navigator.mediaDevices.enumerateDevices();
    } catch {
      return null;
    }

    let bestId: string | null = null;
    let bestScore = -1;

    for (const cam of devices) {
      if (cam.kind !== 'videoinput' || cam.deviceId === excludeDeviceId) continue;

      const label = (cam.label || '').toLowerCase();
      if (label.includes('front') || label.includes('face') || label.includes('user')) continue;

      let probe: MediaStream | null = null;
      try {
        probe = await this.openDeviceId(cam.deviceId);
        const track = probe.getVideoTracks()[0];
        const caps: any = track?.getCapabilities?.() ?? {};
        const settings: any = track?.getSettings?.() ?? {};
        const facing: string[] = caps.facingMode ?? (settings.facingMode ? [settings.facingMode] : []);

        if (facing.includes('user') && !facing.includes('environment')) continue; // front lens

        const score = this.scoreCamera(caps);
        if (this.verbose) console.log('vin-extractor probe', cam.label || cam.deviceId, { score, caps });
        if (score > bestScore) {
          bestScore = score;
          bestId = settings.deviceId || cam.deviceId;
        }
      } catch (error) {
        if (this.verbose) console.warn('vin-extractor probe failed', cam.label || cam.deviceId, error);
      } finally {
        probe?.getTracks().forEach(t => t.stop());
      }
    }

    return bestId;
  };

  // A lens can scan a close-up QR/VIN only if it autofocuses, so autofocus is
  // decisive; then sensor resolution (main lens ≫ ultrawide/macro); then torch as
  // a weak tiebreak (usually only the main lens exposes it). FOV isn't exposed by
  // the API, so resolution stands in for "is this the main lens".
  private lensAutofocuses = (caps: any): boolean => {
    const modes: string[] = caps?.focusMode ?? [];
    return modes.includes('continuous') || modes.includes('single-shot') || !!caps?.focusDistance;
  };

  private scoreCamera = (caps: any): number => {
    const resolution = (caps?.width?.max ?? 0) * (caps?.height?.max ?? 0);
    return (this.lensAutofocuses(caps) ? 1e12 : 0) + resolution + (caps?.torch ? 1e6 : 0);
  };

  startCamera = async () => {
    try {
      this.streamRef = await this.getCameraStream(this.facingMode);

      if (this.videoPlayer) {
        this.videoPlayer.srcObject = this.streamRef;
        try {
          await this.videoPlayer.play();
        } catch (_) {
          // play() can reject when interrupted (e.g. the modal closes again
          // before the stream is ready) — safe to swallow.
        }
      }

      this.manualCaptureLoading = false;

      // Fire-and-forget: autofocus is a best-effort enhancement and must never
      // block or alter the existing open/capture flow if it stalls or fails.
      this.applyAutofocus();
    } catch (error) {
      if (this.verbose) console.error(error);
      throw new Error('Error accessing camera: ');
    }
  };

  // Low-end Android cameras often open with a fixed/idle focus mode, so the live
  // preview never sharpens — whereas iOS autofocuses natively, which is why it
  // "just works" on iPhone. When the track advertises focus control, explicitly
  // request continuous autofocus (falling back to single-shot). Everything here
  // is feature-detected and best-effort; unsupported devices keep their default.
  applyAutofocus = async () => {
    try {
      const track = this.streamRef?.getVideoTracks?.()[0];
      if (!track || typeof track.getCapabilities !== 'function') return;

      const capabilities: any = track.getCapabilities();
      const focusModes: string[] = capabilities?.focusMode ?? [];

      if (this.verbose) {
        console.log('vin-extractor camera capabilities', capabilities);
        console.log('vin-extractor camera settings', track.getSettings?.());
      }

      if (focusModes.includes('continuous')) await track.applyConstraints({ advanced: [{ focusMode: 'continuous' } as any] });
      else if (focusModes.includes('single-shot')) await track.applyConstraints({ advanced: [{ focusMode: 'single-shot' } as any] });
    } catch (error) {
      if (this.verbose) console.warn('vin-extractor autofocus not applied', error);
    }
  };

  closeScanner = () => {
    this.isCameraReady = false;
    this.manualCaptureLoading = false;
    this.abortController?.abort();
    clearTimeout(this.frameCaptureTimeoutRef);
    if (this.codeReader) this.codeReader.reset();
    if (this.videoPlayer) {
      try {
        this.videoPlayer.pause();
      } catch (_) {
        /* ignore */
      }
      this.videoPlayer.srcObject = null;
    }
    if (document) document.body.style.overflow = 'auto';
    this.containerAnimation = 'hide-container';
  };

  stopCamera = () => {
    if (this.streamRef) {
      this.streamRef.getTracks().forEach(track => track.stop());
      this.streamRef = null;
    }
  };

  @Watch('isOpen')
  isOpenHandler(newValue: boolean) {
    if (newValue) this.openScanner();
    else this.closeScanner();

    this.triggerCallback(this.onOpenChange, newValue);
  }

  @Watch('readSticker')
  QRChanged(newValue: boolean) {
    if (newValue) this.componentDidLoad();
  }

  switchCamera = () => {
    if (this.videoInputs.length <= 1) return;

    this.facingMode = this.facingMode === 'environment' ? 'user' : 'environment';

    localStorage.setItem(FACING_MODE_KEY, this.facingMode);

    this.stopCamera();
    this.startCamera();
    this.switchRotateDegree += 90;
  };

  @Watch('isAnimating')
  animationHandler(newValue: boolean) {
    if (!newValue && !this.isOpen) this.stopCamera();
  }
  render() {
    const ariaExpanded = this.isOpen && this.isCameraReady && (this.useOcr || this.readSticker);

    return (
      <Host translate="no">
        <slot />
        <input class="vin-extractor-input" type="file" accept="image/*" aria-label="Upload VIN image" {...(this.captureEnvironment ? { capture: 'environment' } : {})} hidden />
        <canvas class="video-canvas hidden" aria-hidden="true"></canvas>
        {!this.uploaderButtonId && (
          <div
            onClick={() => (this.isOpen = false)}
            role="dialog"
            aria-modal={ariaExpanded.toString()}
            aria-label={this.title || 'VIN Scanner'}
            aria-hidden={(!ariaExpanded).toString()}
            class="vin-extractor-background md:transition-all md:duration-300 fixed flex items-center justify-center w-[100dvw] h-[100dvh] top-0 left-0 z-[9999]"
          >
            <div
              role="document"
              onClick={e => e.stopPropagation()}
              aria-expanded={ariaExpanded.toString()}
              onAnimationEnd={() => (this.isAnimating = false)}
              onAnimationStart={() => (this.isAnimating = true)}
              class={cn(
                'vin-extractor-container md:w-[600px] md:rounded-lg md:overflow-hidden opacity-0 md:h-auto w-full h-full relative transition-all duration-500',
                this.containerAnimation,
              )}
            >
              <div class="vin-extractor-heading items-center md:py-[8px] w-full md:!opacity-100 md:!translate-x-0 p-[16px] md:bg-white bg-black/30 shadow-md z-10 md:relative absolute top-0 left-0 flex justify-between">
                {this.videoInputs.length > 1 ? (
                  <button
                    type="button"
                    aria-label="Switch camera"
                    onClick={this.switchCamera}
                    class="size-[32px] md:border-none md:bg-white md:hover:bg-slate-100 bg-slate-100 rounded-lg p-1 hover:text-slate-700 border transition-colors duration-300 hover:bg-slate-300 border-slate-600 text-slate-600 hover:border-slate-700"
                  >
                    <svg
                      width="24"
                      height="24"
                      fill="none"
                      stroke-width="2"
                      aria-hidden="true"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      xmlns="http://www.w3.org/2000/svg"
                      class="size-full transition-all duration-300"
                      style={{ rotate: `${this.switchRotateDegree}deg` }}
                    >
                      <path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8" />
                      <path d="M21 3v5h-5" />
                      <path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16" />
                      <path d="M8 16H3v5" />
                    </svg>
                  </button>
                ) : (
                  <div class="size-8" aria-hidden="true" />
                )}
                <h1 class="text-center text-[18px] md:text-[24px] md:text-black text-slate-100 form-input-label" part="form-input-label">
                  {this.title}
                </h1>
                <button
                  type="button"
                  aria-label="Close scanner"
                  onClick={() => (this.isOpen = false)}
                  class="size-[32px] md:border-none md:bg-white md:hover:bg-slate-100 bg-slate-100 rounded-lg p-1 hover:text-slate-700 border transition-colors duration-300 hover:bg-slate-300 border-slate-600 text-slate-600 hover:border-slate-700"
                >
                  <svg
                    fill="none"
                    stroke-width="2"
                    class="size-full"
                    aria-hidden="true"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path d="M18 6 6 18" />
                    <path d="m6 6 12 12" />
                  </svg>
                </button>
              </div>
              {this.manualCapture && !!this.manualCaptureHint && !this.manualCaptureLoading && (
                <p
                  aria-hidden="true"
                  class="vin-extractor-manual-capture-hint absolute left-1/2 -translate-x-1/2 bottom-[88px] z-10 m-0 px-3 py-1 rounded-full bg-black/55 text-white text-center text-[13px] leading-[1.3] whitespace-nowrap pointer-events-none"
                >
                  {this.manualCaptureHint}
                </p>
              )}
              {this.manualCapture && (
                <button
                  type="button"
                  part="vin-extractor-capture-button"
                  disabled={this.manualCaptureLoading}
                  onClick={this.captureFrame.bind(this, true)}
                  aria-label={this.manualCaptureLoading ? 'Capturing...' : this.manualCaptureLabel || 'Capture VIN'}
                  class={cn(
                    'vin-extractor-capture-button absolute disabled:bg-white/75 outline-none cursor-pointer left-1/2 -translate-x-1/2 flex justify-center items-center h-[60px] py-[10px] rounded-full shadow-lg border border-slate-500 text-slate-500 z-10 bg-white bottom-4',
                    this.manualCaptureLabel ? 'px-6 min-w-[140px] gap-2 text-[15px] font-medium whitespace-nowrap' : 'w-[100px]',
                  )}
                >
                  {this.manualCaptureLoading ? (
                    <svg
                      fill="none"
                      stroke-width="2"
                      aria-hidden="true"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      xmlns="http://www.w3.org/2000/svg"
                      class={cn('animate-spin animate-spin-v', this.manualCaptureLabel ? 'size-[22px]' : 'size-full')}
                    >
                      <path d="M21 12a9 9 0 1 1-6.219-8.56" />
                    </svg>
                  ) : this.manualCaptureLabel ? (
                    <span>{this.manualCaptureLabel}</span>
                  ) : (
                    <svg
                      fill="none"
                      stroke-width="2"
                      class="size-full"
                      aria-hidden="true"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path d="M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3l-2.5-3z" />
                      <circle cx="12" cy="13" r="3" />
                    </svg>
                  )}
                </button>
              )}
              <video
                autoPlay
                id="video"
                playsInline
                aria-label="Camera preview"
                class="video-player aspect-video bg-black min-w-full min-h-full object-cover object-center"
              ></video>
            </div>
          </div>
        )}
      </Host>
    );
  }
}
