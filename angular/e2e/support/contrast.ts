import type { Locator } from '@playwright/test';

/**
 * The contrast between an element's text and what is actually behind it.
 *
 * <p>
 * Written because two colours on the candidate's screen measured 1.01:1 and
 * 1.06:1 in dark mode — the answer they had just chosen, and the sentence saying
 * whether they had passed. Both were invisible, and no test noticed, because a
 * test that asks "is the element visible" gets `true` for white text on white.
 * </p>
 * <p>
 * The background is resolved by walking up until something is not transparent,
 * which is what the eye does: a transparent element shows whatever its parent
 * paints, and reading `background-color` off the element alone reports
 * `rgba(0,0,0,0)` and proves nothing.
 * </p>
 */
export async function contrastRatio(locator: Locator): Promise<number> {
  return locator.evaluate(element => {
    const channel = (value: number) => {
      const v = value / 255;
      return v <= 0.04045 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
    };

    const parse = (colour: string): [number, number, number, number] => {
      const parts = colour.match(/[\d.]+/g)?.map(Number) ?? [0, 0, 0, 0];
      return [parts[0] ?? 0, parts[1] ?? 0, parts[2] ?? 0, parts[3] ?? 1];
    };

    const luminance = ([r, g, b]: number[]) =>
      0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);

    const behind = (start: Element): [number, number, number] => {
      // Every painted layer from the element outwards, nearest first. A
      // translucent panel is not its own colour and it is not transparent
      // either — it is itself composited over whatever it sits on, and taking
      // either shortcut gives a number that is not what the eye receives.
      const layers: [number, number, number, number][] = [];

      for (let node: Element | null = start; node; node = node.parentElement) {
        const layer = parse(getComputedStyle(node).backgroundColor);

        if (layer[3] > 0) {
          layers.push(layer);

          if (layer[3] >= 0.999) {
            break;
          }
        }
      }

      let [r, g, b] = [255, 255, 255];

      // Furthest first, so each nearer layer is painted over the result.
      for (let i = layers.length - 1; i >= 0; i--) {
        const [lr, lg, lb, la] = layers[i];

        r = lr * la + r * (1 - la);
        g = lg * la + g * (1 - la);
        b = lb * la + b * (1 - la);
      }

      return [r, g, b];
    };

    const text = parse(getComputedStyle(element).color);
    const back = behind(element);

    const lighter = Math.max(luminance(text), luminance(back));
    const darker = Math.min(luminance(text), luminance(back));

    return Math.round(((lighter + 0.05) / (darker + 0.05)) * 100) / 100;
  });
}
