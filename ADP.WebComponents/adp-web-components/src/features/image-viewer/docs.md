```typescript
  // #region Image Viewer Logic

  @State() expandedImage?: string = '';

  originalImage: HTMLImageElement;

  // #endregion  Image Viewer Logic
//   <ImageViewer closeImageViewer={() => closeImageViewer.bind(this)()} expandedImage={this.expandedImage} />

// <button
//   onClick={({ target }) => openImageViewer.bind(this)(target as HTMLImageElement, accessory.image)}
//   class="shrink-0 relative ring-0 outline-none w-fit mx-auto [&_img]:hover:shadow-lg [&_div]:hover:!opacity-100 cursor-pointer"
// >
//   <div class="absolute flex-col justify-center gap-[4px] size-full flex items-center pointer-events-none hover:opacity-100 rounded-lg opacity-0 bg-black/40 transition-all duration-300">
//     <img src={Eye} />
//     <span class="text-white">{texts.expand}</span>
//   </div>
// <img class="w-auto h-auto max-w-[100px] max-h-[100px] cursor-pointer shadow-sm rounded-lg transition-all duration-300" src={accessory.image} />
// </button>
```
