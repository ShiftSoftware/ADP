import { useCallback, useEffect, useRef, useState } from 'react';
import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

const WIDTH = 480;
const HEIGHT = 160;

/** Canvas-based signature capture. Records a PNG data URL into the answer map on
 *  pointer-up. Minimal on purpose — a full signature library (pressure, smoothing)
 *  can swap in later via a custom registry component. */
export function SignatureQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer, ui } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [drawing, setDrawing] = useState(false);
  const [hasInk, setHasInk] = useState(Boolean(answers[id]));

  const getCtx = () => canvasRef.current?.getContext('2d') ?? null;

  const getPos = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const rect = (e.target as HTMLCanvasElement).getBoundingClientRect();
    return {
      x: ((e.clientX - rect.left) / rect.width) * WIDTH,
      y: ((e.clientY - rect.top) / rect.height) * HEIGHT,
    };
  };

  const commit = useCallback(() => {
    const data = canvasRef.current?.toDataURL('image/png');
    if (data) setAnswer(id, data);
  }, [id, setAnswer]);

  const clear = () => {
    const ctx = getCtx();
    if (!ctx) return;
    ctx.clearRect(0, 0, WIDTH, HEIGHT);
    setHasInk(false);
    setAnswer(id, null);
  };

  useEffect(() => {
    const ctx = getCtx();
    if (!ctx) return;
    ctx.lineWidth = 2;
    ctx.lineCap = 'round';
    ctx.strokeStyle = '#111';
  }, []);

  return (
    <div className="survey-question survey-question--signature">
      <div className="survey-question__label">
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </div>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <canvas
        ref={canvasRef}
        className="survey-question__signature-canvas"
        width={WIDTH}
        height={HEIGHT}
        role="img"
        aria-label="signature pad"
        onPointerDown={(e) => {
          (e.target as HTMLCanvasElement).setPointerCapture(e.pointerId);
          const ctx = getCtx();
          if (!ctx) return;
          const { x, y } = getPos(e);
          ctx.beginPath();
          ctx.moveTo(x, y);
          setDrawing(true);
        }}
        onPointerMove={(e) => {
          if (!drawing) return;
          const ctx = getCtx();
          if (!ctx) return;
          const { x, y } = getPos(e);
          ctx.lineTo(x, y);
          ctx.stroke();
          setHasInk(true);
        }}
        onPointerUp={() => {
          setDrawing(false);
          if (hasInk) commit();
        }}
      />
      <div className="survey-question__signature-actions">
        <button type="button" className="survey-button survey-button--ghost" onClick={clear}>
          {ui.clearSignature}
        </button>
      </div>
    </div>
  );
}
