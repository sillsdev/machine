export const LOG_ONE: number = 0;
export const LOG_ZERO: number = -999999999;

export function logAdd(logx: number, logy: number): number {
  if (logx > logy) {
    return logx + Math.log(1 + Math.exp(logy - logx));
  }
  return logy + Math.log(1 + Math.exp(logx - logy));
}

export function logMultiply(logx: number, logy: number): number {
  let result = logx + logy;
  if (result < LOG_ZERO) {
    result = LOG_ZERO;
  }
  return result;
}

export function logDivide(logx: number, logy: number): number {
  let result = logx - logy;
  if (result < LOG_ZERO) {
    result = LOG_ZERO;
  }
  return result;
}

export function toLogSpace(value: number): number {
  return Math.log(value);
}

export function toStandardSpace(logValue: number): number {
  return Math.exp(logValue);
}
